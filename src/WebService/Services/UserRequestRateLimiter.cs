using Microsoft.Extensions.Caching.Memory;

namespace WebService.Services;

public sealed class UserRequestRateLimiter(
	IAppConfiguration configuration,
	IMemoryCache memoryCache) : IUserRequestRateLimiter
{
	public bool TryIncrement(long userId)
	{
		var now = DateTimeOffset.UtcNow;
        var cacheKey = GetCacheKey(userId);
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = configuration.AiRequestLimitPeriod
        };
		var state = memoryCache.GetOrCreate(cacheKey, cacheEntry =>
		{
			cacheEntry.SetSlidingExpiration(configuration.AiRequestLimitPeriod);
			return UserState.CreateNew();
		})!;

        switch (state)
        {
            case { BlockedUntilUtc: { } blockedUntilUtc } when now < blockedUntilUtc:
                return false;
            case { BlockedUntilUtc: { } }:
                state = UserState.CreateNew();
                memoryCache.Set(cacheKey, state, cacheEntryOptions);
                return true;
        }

        state = state with { Count = state.Count + 1 };
        if (state is { Count: var count } && count > configuration.AiRequestLimitPerUser)
        {
            state = state with { BlockedUntilUtc = now + configuration.AiRequestLimitPeriod };
            memoryCache.Set(cacheKey, state, cacheEntryOptions);

            return false;
        }

        memoryCache.Set(cacheKey, state, cacheEntryOptions);

        return true;
	}

	public UserRateLimitStatus GetStatus(long userId)
	{
		var now = DateTimeOffset.UtcNow;
		var state = memoryCache.Get<UserState>(GetCacheKey(userId));

		if (state is null)
			return new UserRateLimitStatus(configuration.AiRequestLimitPerUser, null);

		if (state.BlockedUntilUtc is { } blockedUntil && now < blockedUntil)
			return new UserRateLimitStatus(0, blockedUntil);

		var remaining = Math.Max(0, configuration.AiRequestLimitPerUser - state.Count);
		return new UserRateLimitStatus(remaining, null);
	}

	private static string GetCacheKey(long userId) => $"telegram-user-request-rate-limit:{userId}";

    private sealed record UserState(
        int Count,
        DateTimeOffset? BlockedUntilUtc)
    {
        public static UserState CreateNew() => new(
            Count: 0,
            BlockedUntilUtc: null);
    }
}
   	