using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using Telegram.Bot;
using WebService.Services;

internal static class ServiceCollectionExtensions
{
	public static IServiceCollection AddAppConfiguration(this IServiceCollection services)
	{
		services.AddSingleton<IAppConfigurationProvider, AppConfigurationProvider>();
		services.AddSingleton<IAppConfiguration>(serviceProvider =>
			serviceProvider.GetRequiredService<IAppConfigurationProvider>().Create());

		return services;
	}

	public static IServiceCollection AddTelegramBotClient(this IServiceCollection services)
	{
		services.AddSingleton<ITelegramBotClient>(serviceProvider =>
		{
			var appConfiguration = serviceProvider.GetRequiredService<IAppConfiguration>();
			return new TelegramBotClient(appConfiguration.TelegramBotToken);
		});

		return services;
	}

	public static IServiceCollection AddChatGptAgent(this IServiceCollection services)
	{
		services.AddSingleton<AIAgent>(serviceProvider =>
		{
			var appConfiguration = serviceProvider.GetRequiredService<IAppConfiguration>();
			var apiKey = appConfiguration.OpenAiApiKey;
			var model = appConfiguration.ChatGptModel;
			var systemPrompt = appConfiguration.ChatGptSystemPrompt;
			ChatClient chatClient = new OpenAIClient(apiKey).GetChatClient(model);

			return chatClient.AsAIAgent(
				instructions: systemPrompt,
				name: "TelegramChatGptForwarder");
		});

		return services;
	}

	public static IServiceCollection AddTelegramWebhookInitialization(this IServiceCollection services)
	{
		services.AddHostedService<TelegramWebhookInitializerBackgroundService>();

		return services;
	}
}
