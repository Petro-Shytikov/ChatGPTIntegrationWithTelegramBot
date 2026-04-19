using System.ComponentModel.DataAnnotations;

public sealed class AppConfiguration : IAppConfiguration
{
	public AppConfiguration(
		string telegramBotToken,
		string telegramPublicWebhookUrl,
		string telegramWebhookSecretToken,
		string openAiApiKey,
		string chatGptModel,
		string chatGptSystemPrompt)
    {
        TelegramBotToken = telegramBotToken;
        TelegramPublicWebhookUrl = telegramPublicWebhookUrl;
        TelegramWebhookSecretToken = telegramWebhookSecretToken;
        OpenAiApiKey = openAiApiKey;
        ChatGptModel = chatGptModel;
        ChatGptSystemPrompt = chatGptSystemPrompt;
    }

	[Required]
	public string TelegramBotToken { get;}

    [Required]
	public string TelegramPublicWebhookUrl { get; }

	[Required]
	public string TelegramWebhookSecretToken { get; }

	[Required]
	public string OpenAiApiKey { get; }

	[Required]
	public string ChatGptModel { get; }

	[Required]
	public string ChatGptSystemPrompt { get; }
}
