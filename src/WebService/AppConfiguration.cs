using System.ComponentModel.DataAnnotations;

public sealed class AppConfiguration : IAppConfiguration, IValidatableObject
{
	public AppConfiguration(
		string telegramBotToken,
		string telegramPublicWebhookUrl,
		string telegramWebhookSecretToken,
		string openAiApiKey,
		string chatGptModel,
		string chatGptSystemPrompt,
		TimeSpan retryTelegramWebhookInitializerDelay,
		int maxTelegramRequestLength,
		int aiRequestLimitPerUser,
		TimeSpan aiRequestLimitPeriod)
    {
        TelegramBotToken = telegramBotToken;
        TelegramPublicWebhookUrl = telegramPublicWebhookUrl;
        TelegramWebhookSecretToken = telegramWebhookSecretToken;
        OpenAiApiKey = openAiApiKey;
        ChatGptModel = chatGptModel;
        ChatGptSystemPrompt = chatGptSystemPrompt;
		RetryTelegramWebhookInitializerDelay = retryTelegramWebhookInitializerDelay;
		MaxTelegramRequestLength = maxTelegramRequestLength;
		AiRequestLimitPerUser = aiRequestLimitPerUser;
		AiRequestLimitPeriod = aiRequestLimitPeriod;
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

	[Required]
	public TimeSpan RetryTelegramWebhookInitializerDelay { get; }

	[Range(1, int.MaxValue)]
	public int MaxTelegramRequestLength { get; }

	[Range(1, int.MaxValue)]
	public int AiRequestLimitPerUser { get; }

	[Required]
	public TimeSpan AiRequestLimitPeriod { get; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (string.IsNullOrWhiteSpace(TelegramBotToken))
		{
			yield return CreateValidationResult(
				$"{nameof(TelegramBotToken)} must not be empty or whitespace.",
				nameof(TelegramBotToken));
		}

		if (string.IsNullOrWhiteSpace(TelegramPublicWebhookUrl))
		{
			yield return CreateValidationResult(
				$"{nameof(TelegramPublicWebhookUrl)} must not be empty or whitespace.",
				nameof(TelegramPublicWebhookUrl));
		}

		if (string.IsNullOrWhiteSpace(TelegramWebhookSecretToken))
		{
			yield return CreateValidationResult(
				$"{nameof(TelegramWebhookSecretToken)} must not be empty or whitespace.",
				nameof(TelegramWebhookSecretToken));
		}

		if (string.IsNullOrWhiteSpace(OpenAiApiKey))
		{
			yield return CreateValidationResult(
				$"{nameof(OpenAiApiKey)} must not be empty or whitespace.",
				nameof(OpenAiApiKey));
		}

		if (string.IsNullOrWhiteSpace(ChatGptModel))
		{
			yield return CreateValidationResult(
				$"{nameof(ChatGptModel)} must not be empty or whitespace.",
				nameof(ChatGptModel));
		}

		if (string.IsNullOrWhiteSpace(ChatGptSystemPrompt))
		{
			yield return CreateValidationResult(
				$"{nameof(ChatGptSystemPrompt)} must not be empty or whitespace.",
				nameof(ChatGptSystemPrompt));
		}

		if (RetryTelegramWebhookInitializerDelay <= TimeSpan.Zero)
		{
			yield return CreateValidationResult(
				$"{nameof(RetryTelegramWebhookInitializerDelay)} must be greater than zero.",
				nameof(RetryTelegramWebhookInitializerDelay));
		}

		if (AiRequestLimitPeriod <= TimeSpan.Zero)
		{
			yield return CreateValidationResult(
				$"{nameof(AiRequestLimitPeriod)} must be greater than zero.",
				nameof(AiRequestLimitPeriod));
		}
	}

	private static ValidationResult CreateValidationResult(string errorMessage, string memberName) =>
		new(errorMessage, [memberName]);
}
