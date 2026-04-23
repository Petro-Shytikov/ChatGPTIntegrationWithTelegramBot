namespace WebService.Services;

public record UserRateLimitStatus(int RemainingRequests, DateTimeOffset? BlockedUntil);

public interface IUserRequestRateLimiter
{
	bool TryIncrement(long userId);
	UserRateLimitStatus GetStatus(long userId);
}
