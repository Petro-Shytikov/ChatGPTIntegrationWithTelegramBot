namespace WebService.Services;

public interface IUserRequestRateLimiter
{
	bool TryIncrement(long userId);
}
