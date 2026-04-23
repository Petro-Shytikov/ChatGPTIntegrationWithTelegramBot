using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using WebService.Filters;
using WebService.Helpers;
using WebService.Services;

namespace WebService.Controllers;

[ApiController]
[Route("telegram")]
public sealed class TelegramController : ControllerBase
{
	private readonly ITelegramBotClient _botClient;
	private readonly AIAgent _aiAgent;
	private readonly IUserRequestRateLimiter _rateLimiter;
	private readonly IAppConfiguration _configuration;
	private readonly ILogger<TelegramController> _logger;

	public TelegramController(
		ITelegramBotClient botClient,
		AIAgent aiAgent,
		IUserRequestRateLimiter rateLimiter,
		IAppConfiguration configuration,
		ILogger<TelegramController> logger)
	{
		_botClient = botClient;
		_aiAgent = aiAgent;
		_rateLimiter = rateLimiter;
		_configuration = configuration;
		_logger = logger;
	}

	[ServiceFilter(typeof(ValidateTelegramSecretFilter))]
	[ServiceFilter(typeof(ValidateTelegramMessagePayloadFilter))]
	[ServiceFilter(typeof(ValidateTelegramMessageLengthFilter))]
	[HttpPost("webhook")]
	public async Task<IActionResult> Webhook([FromBody] Update update, CancellationToken cancellationToken)
	{
		return await (TelegramBotHelper.TryHandle(update) switch
		{
			TelegramBotCommandType.Info => SendInfoMessageAsync(update, cancellationToken),
			TelegramBotCommandType.Ask or TelegramBotCommandType.None => ProcessAiRequestAsync(update, cancellationToken),
			_ => SendMessageAsync(update, cancellationToken)
		});
	}

	private async Task<IActionResult> SendInfoMessageAsync(Update update, CancellationToken cancellationToken)
	{
		var chatId = update.Message!.Chat!.Id;
		var userRateLimitStatus = _rateLimiter.GetStatus(chatId);
		var infoText = TelegramBotHelper.BuildInfoReply(userRateLimitStatus, _configuration);
		await _botClient.SendMessage(chatId, infoText, cancellationToken: cancellationToken);
		return Ok();
	}

	private async Task<IActionResult> SendMessageAsync(Update update, CancellationToken cancellationToken)
	{
		var chatId = update.Message!.Chat!.Id;
		var text = $"Unknown command: {update.Message!.Text!.Split(' ', 2)[0].ToLowerInvariant()}";
		await _botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
		return Ok();
	}

	private async Task<IActionResult> ProcessAiRequestAsync(Update update, CancellationToken cancellationToken)
	{
		var message = update.Message!;
		var text = message.Text!;
		var chatId = message.Chat!.Id;

		// /ask <query> or plain message: enforce rate limit before forwarding to AI.
		if (!_rateLimiter.TryIncrement(chatId))
		{
			_logger.LogInformation(
				"Telegram update {UpdateId} rejected: user {UserId} exceeded rate limit of {Limit} requests per {Period}.",
				update.Id,
				chatId,
				_configuration.AiRequestLimitPerUser,
				_configuration.AiRequestLimitPeriod);

			await _botClient.SendMessage(
				chatId,
				$"You have reached the limit of {_configuration.AiRequestLimitPerUser} requests per {_configuration.AiRequestLimitPeriod.TotalHours:0} hour(s). Please try again later.",
				cancellationToken: cancellationToken);

			return Ok();
		}

		// Strip "/ask " prefix when present; otherwise use the full message text.
		var userText = text.StartsWith("/ask ", StringComparison.OrdinalIgnoreCase)
			? text["/ask ".Length..].Trim()
			: text;

		try
		{
			var aiResponse = await _aiAgent.RunAsync(userText, cancellationToken: cancellationToken);
			var responseText = aiResponse?.ToString();
			if (string.IsNullOrWhiteSpace(responseText))
			{
				responseText = "I could not generate a response right now.";
			}

			await _botClient.SendMessage(chatId, responseText, cancellationToken: cancellationToken);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Failed to process Telegram update {UpdateId}", update.Id);
			await _botClient.SendMessage(
				chatId,
				"Sorry, I could not process your request right now.",
				cancellationToken: cancellationToken);
		}

		return Ok();
	}
}

