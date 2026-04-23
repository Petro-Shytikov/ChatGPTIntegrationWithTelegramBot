using Telegram.Bot.Types;
using WebService.Helpers;
using WebService.Services;

namespace WebService.Tests;

public class TelegramBotHelperTests
{
	[Test]
	[Arguments(null, TelegramBotCommandType.None)]
	[Arguments("", TelegramBotCommandType.None)]
	[Arguments("   ", TelegramBotCommandType.None)]
	[Arguments("hello", TelegramBotCommandType.None)]
	[Arguments("/ask", TelegramBotCommandType.Ask)]
	[Arguments("/ask tell me a joke", TelegramBotCommandType.Ask)]
	[Arguments("/ASK tell me a joke", TelegramBotCommandType.Ask)]
	[Arguments("/info", TelegramBotCommandType.Info)]
	[Arguments("/INFO", TelegramBotCommandType.Info)]
	[Arguments("/unknown", TelegramBotCommandType.Unknown)]
	public async Task TryHandle_WhenMessageTextVaries_ReturnsExpectedCommandType(string? messageText, TelegramBotCommandType expected)
	{
		var update = CreateUpdate(messageText);

		var result = TelegramBotHelper.TryHandle(update);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task BuildInfoReply_WhenUserIsNotBlocked_ContainsConfigurationAndRemainingRequests()
	{
		var configuration = CreateConfiguration(aiRequestLimitPerUser: 10, aiRequestLimitPeriod: TimeSpan.FromHours(24));
		var status = new UserRateLimitStatus(RemainingRequests: 7, BlockedUntil: null);

		var text = TelegramBotHelper.BuildInfoReply(status, configuration);

		await Assert.That(text).Contains("Service Version:");
		await Assert.That(text).Contains("AI Model: gpt-5.4-mini");
		await Assert.That(text).Contains("Max Request Length: 1500 characters");
		await Assert.That(text).Contains("Available Requests: 7 of 10");
		await Assert.That(text).Contains("Request Limit Period: 24 hour(s)");
	}

	[Test]
	public async Task BuildInfoReply_WhenUserIsBlocked_ContainsBlockedUntilAndZeroAvailableRequests()
	{
		var configuration = CreateConfiguration(aiRequestLimitPerUser: 10, aiRequestLimitPeriod: TimeSpan.FromHours(24));
		var blockedUntil = new DateTimeOffset(2026, 4, 23, 15, 30, 0, TimeSpan.Zero);
		var status = new UserRateLimitStatus(RemainingRequests: 0, BlockedUntil: blockedUntil);

		var text = TelegramBotHelper.BuildInfoReply(status, configuration);

		await Assert.That(text).Contains("Available Requests: 0 of 10 (blocked until 2026-04-23 15:30 UTC)");
	}

	private static Update CreateUpdate(string? messageText) =>
		new()
		{
			Message = new Message
			{
				Text = messageText
			}
		};

	private static IAppConfiguration CreateConfiguration(int aiRequestLimitPerUser, TimeSpan aiRequestLimitPeriod) =>
		new AppConfiguration(
			telegramBotToken: "test-telegram-token",
			telegramPublicWebhookUrl: "https://example.com/webhook",
			telegramWebhookSecretToken: "test-secret-token",
			openAiApiKey: "test-openai-key",
			chatGptModel: "gpt-5.4-mini",
			chatGptSystemPrompt: "You are a helpful assistant.",
			retryTelegramWebhookInitializerDelay: TimeSpan.FromSeconds(1),
			maxTelegramRequestLength: 1500,
			aiRequestLimitPerUser: aiRequestLimitPerUser,
			aiRequestLimitPeriod: aiRequestLimitPeriod);
}
