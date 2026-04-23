using Microsoft.Extensions.Caching.Memory;

namespace WebService.Services;

internal sealed class UserRequestRateLimiter(
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
   	