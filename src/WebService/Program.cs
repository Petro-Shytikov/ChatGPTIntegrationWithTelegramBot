using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.Configure<ChatGptOptions>(builder.Configuration.GetSection("ChatGpt"));

builder.Services.AddSingleton<ITelegramBotClient>(serviceProvider =>
{
	var botToken = GetRequiredEnvironmentVariable("TELEGRAM_BOT_TOKEN");
	return new TelegramBotClient(botToken);
});

builder.Services.AddSingleton<AIAgent>(serviceProvider =>
{
	var options = serviceProvider.GetRequiredService<IOptions<ChatGptOptions>>().Value;
	var apiKey = GetRequiredEnvironmentVariable("OPENAI_API_KEY");

	if (string.IsNullOrWhiteSpace(options.Model))
	{
		throw new InvalidOperationException("ChatGpt:Model is required.");
	}

	ChatClient chatClient = new OpenAIClient(apiKey).GetChatClient(options.Model);

	return chatClient.AsAIAgent(
		instructions: options.SystemPrompt,
		name: "TelegramChatGptForwarder");
});

var app = builder.Build();

app.MapGet("/", () => "Telegram ChatGPT integration service is running.");
app.MapHealthChecks("/health");

app.MapPost(
	"/telegram/webhook",
	async (
		HttpRequest request,
		Update update,
		ITelegramBotClient botClient,
		AIAgent aiAgent,
		ILogger<Program> logger,
		CancellationToken cancellationToken) =>
	{
		if (!IsValidTelegramSecret(request, GetOptionalEnvironmentVariable("TELEGRAM_WEBHOOK_SECRET_TOKEN")))
		{
			return Results.Unauthorized();
		}

		if (update.Type != UpdateType.Message || update.Message?.Text is not { Length: > 0 } userText)
		{
			return Results.Ok();
		}

		if (update.Message.Chat is null)
		{
			return Results.Ok();
		}

		try
		{
			var aiResponse = await aiAgent.RunAsync(userText, cancellationToken: cancellationToken);
			var responseText = aiResponse?.ToString();
			if (string.IsNullOrWhiteSpace(responseText))
			{
				responseText = "I could not generate a response right now.";
			}

			await botClient.SendMessage(update.Message.Chat.Id, responseText, cancellationToken: cancellationToken);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Failed to process Telegram update {UpdateId}", update.Id);
			await botClient.SendMessage(
				update.Message.Chat.Id,
				"Sorry, I could not process your request right now.",
				cancellationToken: cancellationToken);
		}

		return Results.Ok();
	});

app.MapPost(
	"/telegram/set-webhook",
	async (
		ITelegramBotClient botClient,
		CancellationToken cancellationToken) =>
	{
		var publicWebhookUrl = GetRequiredEnvironmentVariable("TELEGRAM_PUBLIC_WEBHOOK_URL");
		var webhookSecretToken = GetOptionalEnvironmentVariable("TELEGRAM_WEBHOOK_SECRET_TOKEN");
		var webhookUrl = publicWebhookUrl.TrimEnd('/') + "/telegram/webhook";
		await botClient.SetWebhook(webhookUrl, secretToken: webhookSecretToken, cancellationToken: cancellationToken);

		return Results.Ok(new { webhookUrl });
	});

app.Run();

static bool IsValidTelegramSecret(HttpRequest request, string? expectedSecretToken)
{
	if (string.IsNullOrWhiteSpace(expectedSecretToken))
	{
		return true;
	}

	return request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var providedSecretToken)
		&& string.Equals(providedSecretToken.ToString(), expectedSecretToken, StringComparison.Ordinal);
}

static string GetRequiredEnvironmentVariable(string key)
{
	var value = Environment.GetEnvironmentVariable(key);
	if (string.IsNullOrWhiteSpace(value))
	{
		throw new InvalidOperationException($"Environment variable {key} is required.");
	}

	return value;
}

static string? GetOptionalEnvironmentVariable(string key)
{
	var value = Environment.GetEnvironmentVariable(key);
    return string.IsNullOrWhiteSpace(value) ? null : value;
}

internal sealed class ChatGptOptions
{
	public string Model { get; set; } = "gpt-5.4-mini";

	public string? SystemPrompt { get; set; }
}
