public interface IAppConfiguration
{
	string TelegramBotToken { get; }
	string TelegramPublicWebhookUrl { get; }
	string TelegramWebhookSecretToken { get; }
	string OpenAiApiKey { get; }
	string ChatGptModel { get; }
	string ChatGptSystemPrompt { get; }
	TimeSpan RetryTelegramWebhookInitializerDelay { get; }
	int MaxTelegramRequestLength { get; }
}
