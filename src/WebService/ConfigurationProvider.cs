using System.ComponentModel.DataAnnotations;

public sealed class ConfigurationProvider : IConfigurationProvider
{
	private readonly IConfiguration _configuration;

	public ConfigurationProvider(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public IAppConfiguration Create()
	{
		var appConfiguration = new AppConfiguration(
			telegramBotToken: GetRequiredEnvironmentVariable("TELEGRAM_BOT_TOKEN"),
			telegramPublicWebhookUrl: GetRequiredEnvironmentVariable("TELEGRAM_PUBLIC_WEBHOOK_URL"),
			telegramWebhookSecretToken: GetRequiredEnvironmentVariable("TELEGRAM_WEBHOOK_SECRET_TOKEN"),
			openAiApiKey: GetRequiredEnvironmentVariable("OPENAI_API_KEY"),
			chatGptModel: GetRequiredConfigurationValue<string>(_configuration, "BotSettings:ChatGptModel"),
			chatGptSystemPrompt: GetRequiredConfigurationValue<string>(_configuration, "BotSettings:ChatGptSystemPrompt"),
			retryTelegramWebhookInitializerDelay: GetRequiredConfigurationValue<TimeSpan>(_configuration, "BotSettings:RetryTelegramWebhookInitializerDelay"),
			maxTelegramRequestLength: GetRequiredConfigurationValue<int>(_configuration, "BotSettings:MaxTelegramRequestLength")
		);

		Validate(appConfiguration);

		return appConfiguration;
	}

	private static T GetRequiredConfigurationValue<T>(IConfiguration configuration, string key) =>
		configuration.GetValue<T>(key) ?? throw new NullReferenceException($"Configuration value {key} is required.");

	private static string GetRequiredEnvironmentVariable(string key) =>
		Environment.GetEnvironmentVariable(key) ?? throw new NullReferenceException($"Environment variable {key} is required.");

	private static void Validate(IAppConfiguration appConfiguration) =>
		Validator.ValidateObject(appConfiguration, new ValidationContext(appConfiguration), validateAllProperties: true);
}
