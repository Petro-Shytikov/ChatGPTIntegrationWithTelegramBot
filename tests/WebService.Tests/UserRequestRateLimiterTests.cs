using Microsoft.Extensions.Caching.Memory;
using WebService.Services;

namespace WebService.Tests;

public class UserRequestRateLimiterTests
{
	[Test]
	public async Task TryIncrement_WhenRequestCountIsWithinLimit_ReturnsTrue()
	{
		using var memoryCache = CreateMemoryCache();
		var rateLimiter = CreateRateLimiter(memoryCache, aiRequestLimitPerUser: 2, aiRequestLimitPeriod: TimeSpan.FromSeconds(1));

		var firstResult = rateLimiter.TryIncrement(userId: 42);
		var secondResult = rateLimiter.TryIncrement(userId: 42);

		await Assert.That(firstResult).IsTrue();
		await Assert.That(secondResult).IsTrue();
	}

	[Test]
	public async Task TryIncrement_WhenRequestCountExceedsLimit_ReturnsFalseUntilPeriodExpires()
	{
		using var memoryCache = CreateMemoryCache();
		var rateLimiter = CreateRateLimiter(memoryCache, aiRequestLimitPerUser: 2, aiRequestLimitPeriod: TimeSpan.FromMilliseconds(150));

		var firstResult = rateLimiter.TryIncrement(userId: 42);
		var secondResult = rateLimiter.TryIncrement(userId: 42);
		var thirdResult = rateLimiter.TryIncrement(userId: 42);
		var fourthResult = rateLimiter.TryIncrement(userId: 42);

		await Assert.That(firstResult).IsTrue();
		await Assert.That(secondResult).IsTrue();
		await Assert.That(thirdResult).IsFalse();
		await Assert.That(fourthResult).IsFalse();
	}

	[Test]
	public async Task TryIncrement_WhenBlockPeriodExpires_AllowsUserAgain()
	{
		using var memoryCache = CreateMemoryCache();
		var rateLimiter = CreateRateLimiter(memoryCache, aiRequestLimitPerUser: 1, aiRequestLimitPeriod: TimeSpan.FromMilliseconds(100));

		rateLimiter.TryIncrement(userId: 42);
		var blockedResult = rateLimiter.TryIncrement(userId: 42);
		await Task.Delay(200);
		var resultAfterDelay = rateLimiter.TryIncrement(userId: 42);

		await Assert.That(blockedResult).IsFalse();
		await Assert.That(resultAfterDelay).IsTrue();
	}

	[Test]
	public async Task TryIncrement_WhenSlidingWindowExpiresBeforeLimitIsReached_ResetsCounter()
	{
		using var memoryCache = CreateMemoryCache();
		var rateLimiter = CreateRateLimiter(memoryCache, aiRequestLimitPerUser: 2, aiRequestLimitPeriod: TimeSpan.FromMilliseconds(100));

		var firstResult = rateLimiter.TryIncrement(userId: 42);
		await Task.Delay(200);
		var secondResult = rateLimiter.TryIncrement(userId: 42);
		var thirdResult = rateLimiter.TryIncrement(userId: 42);

		await Assert.That(firstResult).IsTrue();
		await Assert.That(secondResult).IsTrue();
		await Assert.That(thirdResult).IsTrue();
	}

	[Test]
	public async Task TryIncrement_WhenBlockPeriodExpires_RestoresFullQuota()
	{
		using var memoryCache = CreateMemoryCache();
		var rateLimiter = CreateRateLimiter(memoryCache, aiRequestLimitPerUser: 2, aiRequestLimitPeriod: TimeSpan.FromMilliseconds(100));

		rateLimiter.TryIncrement(userId: 42);
		rateLimiter.TryIncrement(userId: 42);
		var blockedResult = rateLimiter.TryIncrement(userId: 42);
		await Task.Delay(200);
		var firstResultAfterDelay = rateLimiter.TryIncrement(userId: 42);
		var secondResultAfterDelay = rateLimiter.TryIncrement(userId: 42);
		var blockedAgainResult = rateLimiter.TryIncrement(userId: 42);

		await Assert.That(blockedResult).IsFalse();
		await Assert.That(firstResultAfterDelay).IsTrue();
		await Assert.That(secondResultAfterDelay).IsTrue();
		await Assert.That(blockedAgainResult).IsFalse();
	}

	[Test]
	public async Task TryIncrement_WhenDifferentUsersCall_TracksEachUserIndependently()
	{
		using var memoryCache = CreateMemoryCache();
		var rateLimiter = CreateRateLimiter(memoryCache, aiRequestLimitPerUser: 1, aiRequestLimitPeriod: TimeSpan.FromSeconds(1));

		var firstUserAllowed = rateLimiter.TryIncrement(userId: 42);
		var firstUserBlocked = rateLimiter.TryIncrement(userId: 42);
		var secondUserAllowed = rateLimiter.TryIncrement(userId: 99);

		await Assert.That(firstUserAllowed).IsTrue();
		await Assert.That(firstUserBlocked).IsFalse();
		await Assert.That(secondUserAllowed).IsTrue();
	}

	private static UserRequestRateLimiter CreateRateLimiter(IMemoryCache memoryCache, int aiRequestLimitPerUser, TimeSpan aiRequestLimitPeriod) =>
		new(
			CreateConfiguration(aiRequestLimitPerUser, aiRequestLimitPeriod),
			memoryCache);

	private static MemoryCache CreateMemoryCache() => new(new MemoryCacheOptions());

	private static IAppConfiguration CreateConfiguration(int aiRequestLimitPerUser, TimeSpan aiRequestLimitPeriod) =>
		new AppConfiguration(
			telegramBotToken: "test-telegram-token",
			telegramPublicWebhookUrl: "https://example.com/webhook",
			telegramWebhookSecretToken: "test-secret-token",
			openAiApiKey: "test-openai-key",
			chatGptModel: "gpt-4.1-mini",
			chatGptSystemPrompt: "You are a helpful assistant.",
			retryTelegramWebhookInitializerDelay: TimeSpan.FromSeconds(1),
			maxTelegramRequestLength: 2048,
			aiRequestLimitPerUser: aiRequestLimitPerUser,
			aiRequestLimitPeriod: aiRequestLimitPeriod);
}