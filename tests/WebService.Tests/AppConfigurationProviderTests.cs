using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using WebService.Services;

namespace WebService.Tests;

[NotInParallel]
public class AppConfigurationProviderTests
{
	private const string DefaultTelegramBotToken = "test-telegram-token";
	private const string DefaultTelegramPublicWebhookUrl = "https://example.com/webhook";
	private const string DefaultTelegramWebhookSecretToken = "test-secret-token";
	private const string DefaultOpenAiApiKey = "test-openai-key";
	private const string DefaultChatGptModel = "gpt-4.1-mini";
	private const string DefaultChatGptSystemPrompt = "You are a helpful assistant.";
	private static readonly TimeSpan DefaultRetryTelegramWebhookInitializerDelay = TimeSpan.FromSeconds(5);
	private const int DefaultMaxTelegramRequestLength = 2048;
	private const int DefaultAiRequestLimitPerUser = 10;
	private static readonly TimeSpan DefaultAiRequestLimitPeriod = TimeSpan.FromMinutes(5);

	private static readonly string[] EnvironmentVariableKeys =
	[
		"TELEGRAM_BOT_TOKEN",
		"TELEGRAM_PUBLIC_WEBHOOK_URL",
		"TELEGRAM_WEBHOOK_SECRET_TOKEN",
		"OPENAI_API_KEY"
	];

	[Test]
	public async Task Create_WhenRequiredValuesExist_ReturnsExpectedAppConfiguration()
	{
		var previousEnvironmentValues = CaptureEnvironmentVariableValues(EnvironmentVariableKeys);

		try
		{
			SetDefaultEnvironmentVariables();

			IConfiguration configuration = CreateConfiguration();

			var provider = new AppConfigurationProvider(configuration);

			var result = provider.Create();

			await Assert.That(result.TelegramBotToken).IsEqualTo(DefaultTelegramBotToken);
			await Assert.That(result.TelegramPublicWebhookUrl).IsEqualTo(DefaultTelegramPublicWebhookUrl);
			await Assert.That(result.TelegramWebhookSecretToken).IsEqualTo(DefaultTelegramWebhookSecretToken);
			await Assert.That(result.OpenAiApiKey).IsEqualTo(DefaultOpenAiApiKey);
			await Assert.That(result.ChatGptModel).IsEqualTo(DefaultChatGptModel);
			await Assert.That(result.ChatGptSystemPrompt).IsEqualTo(DefaultChatGptSystemPrompt);
			await Assert.That(result.RetryTelegramWebhookInitializerDelay).IsEqualTo(DefaultRetryTelegramWebhookInitializerDelay);
			await Assert.That(result.MaxTelegramRequestLength).IsEqualTo(DefaultMaxTelegramRequestLength);
			await Assert.That(result.AiRequestLimitPerUser).IsEqualTo(DefaultAiRequestLimitPerUser);
			await Assert.That(result.AiRequestLimitPeriod).IsEqualTo(DefaultAiRequestLimitPeriod);
		}
		finally
		{
			RestoreEnvironmentVariableValues(previousEnvironmentValues);
		}
	}

	[Test]
	[Arguments("TELEGRAM_BOT_TOKEN")]
	[Arguments("TELEGRAM_PUBLIC_WEBHOOK_URL")]
	[Arguments("TELEGRAM_WEBHOOK_SECRET_TOKEN")]
	[Arguments("OPENAI_API_KEY")]
	public async Task Create_WhenRequiredEnvironmentVariableIsEmpty_ThrowsValidationException(string environmentVariableKey)
	{
		await AssertValidationException(async () =>
		{
			SetDefaultEnvironmentVariables();
			Environment.SetEnvironmentVariable(environmentVariableKey, string.Empty);

			var provider = new AppConfigurationProvider(CreateConfiguration());

			provider.Create();
			await Task.CompletedTask;
		});
	}

	[Test]
	[Arguments("TELEGRAM_BOT_TOKEN")]
	[Arguments("TELEGRAM_PUBLIC_WEBHOOK_URL")]
	[Arguments("TELEGRAM_WEBHOOK_SECRET_TOKEN")]
	[Arguments("OPENAI_API_KEY")]
	public async Task Create_WhenRequiredEnvironmentVariableIsWhitespace_ThrowsValidationException(string environmentVariableKey)
	{
		await AssertValidationException(async () =>
		{
			SetDefaultEnvironmentVariables();
			Environment.SetEnvironmentVariable(environmentVariableKey, "   ");

			var provider = new AppConfigurationProvider(CreateConfiguration());

			provider.Create();
			await Task.CompletedTask;
		});
	}

	[Test]
	[Arguments("BotSettings:ChatGptModel")]
	[Arguments("BotSettings:ChatGptSystemPrompt")]
	public async Task Create_WhenRequiredStringSettingIsEmpty_ThrowsValidationException(string configurationKey)
	{
		await AssertValidationException(async () =>
		{
			SetDefaultEnvironmentVariables();

			var provider = new AppConfigurationProvider(CreateConfiguration(overrides: new Dictionary<string, string?>
			{
				[configurationKey] = string.Empty
			}));

			provider.Create();
			await Task.CompletedTask;
		});
	}

	[Test]
	[Arguments("BotSettings:ChatGptModel")]
	[Arguments("BotSettings:ChatGptSystemPrompt")]
	public async Task Create_WhenRequiredStringSettingIsWhitespace_ThrowsValidationException(string configurationKey)
	{
		await AssertValidationException(async () =>
		{
			SetDefaultEnvironmentVariables();

			var provider = new AppConfigurationProvider(CreateConfiguration(overrides: new Dictionary<string, string?>
			{
				[configurationKey] = "   "
			}));

			provider.Create();
			await Task.CompletedTask;
		});
	}

	[Test]
	[Arguments("BotSettings:MaxTelegramRequestLength", "0")]
	[Arguments("BotSettings:MaxTelegramRequestLength", "-1")]
	[Arguments("BotSettings:AiRequestLimitPerUser", "0")]
	[Arguments("BotSettings:AiRequestLimitPerUser", "-1")]
	public async Task Create_WhenPositiveIntegerSettingIsZeroOrNegative_ThrowsValidationException(string configurationKey, string invalidValue)
	{
		await AssertValidationException(async () =>
		{
			SetDefaultEnvironmentVariables();

			var provider = new AppConfigurationProvider(CreateConfiguration(overrides: new Dictionary<string, string?>
			{
				[configurationKey] = invalidValue
			}));

			provider.Create();
			await Task.CompletedTask;
		});
	}

	[Test]
	[Arguments("BotSettings:RetryTelegramWebhookInitializerDelay", "00:00:00")]
	[Arguments("BotSettings:RetryTelegramWebhookInitializerDelay", "-00:00:01")]
	[Arguments("BotSettings:AiRequestLimitPeriod", "00:00:00")]
	[Arguments("BotSettings:AiRequestLimitPeriod", "-00:00:01")]
	public async Task Create_WhenTimeSpanSettingIsZeroOrNegative_ThrowsValidationException(string configurationKey, string invalidValue)
	{
		await AssertValidationException(async () =>
		{
			SetDefaultEnvironmentVariables();

			var provider = new AppConfigurationProvider(CreateConfiguration(overrides: new Dictionary<string, string?>
			{
				[configurationKey] = invalidValue
			}));

			provider.Create();
			await Task.CompletedTask;
		});
	}

	private static Dictionary<string, string?> CaptureEnvironmentVariableValues(IEnumerable<string> keys)
	{
		var values = new Dictionary<string, string?>();

		foreach (var key in keys)
		{
			values[key] = Environment.GetEnvironmentVariable(key);
		}

		return values;
	}

	private static void RestoreEnvironmentVariableValues(Dictionary<string, string?> values)
	{
		foreach (var pair in values)
		{
			Environment.SetEnvironmentVariable(pair.Key, pair.Value);
		}
	}

	private static IConfiguration CreateConfiguration(Dictionary<string, string?>? overrides = null)
	{
		var values = new Dictionary<string, string?>
		{
			["BotSettings:ChatGptModel"] = DefaultChatGptModel,
			["BotSettings:ChatGptSystemPrompt"] = DefaultChatGptSystemPrompt,
			["BotSettings:RetryTelegramWebhookInitializerDelay"] = DefaultRetryTelegramWebhookInitializerDelay.ToString(),
			["BotSettings:MaxTelegramRequestLength"] = DefaultMaxTelegramRequestLength.ToString(),
			["BotSettings:AiRequestLimitPerUser"] = DefaultAiRequestLimitPerUser.ToString(),
			["BotSettings:AiRequestLimitPeriod"] = DefaultAiRequestLimitPeriod.ToString()
		};

		if (overrides is not null)
		{
			foreach (var pair in overrides)
			{
				values[pair.Key] = pair.Value;
			}
		}

		return new ConfigurationBuilder()
			.AddInMemoryCollection(values)
			.Build();
	}

	private static void SetDefaultEnvironmentVariables()
	{
		Environment.SetEnvironmentVariable("TELEGRAM_BOT_TOKEN", DefaultTelegramBotToken);
		Environment.SetEnvironmentVariable("TELEGRAM_PUBLIC_WEBHOOK_URL", DefaultTelegramPublicWebhookUrl);
		Environment.SetEnvironmentVariable("TELEGRAM_WEBHOOK_SECRET_TOKEN", DefaultTelegramWebhookSecretToken);
		Environment.SetEnvironmentVariable("OPENAI_API_KEY", DefaultOpenAiApiKey);
	}

	private static async Task AssertValidationException(Func<Task> action)
	{
		var previousEnvironmentValues = CaptureEnvironmentVariableValues(EnvironmentVariableKeys);

		try
		{
			await Assert.That(action).Throws<ValidationException>();
		}
		finally
		{
			RestoreEnvironmentVariableValues(previousEnvironmentValues);
		}
	}
}