namespace WebService.Services;

internal interface IUserRequestRateLimiter
{
	bool TryIncrement(long userId);
}
