using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Telegram.Bot;
using Telegram.Bot.Types;
using WebService.Services;

namespace WebService.Filters;

internal sealed class ValidateTelegramRateLimitFilter(
	IUserRequestRateLimiter rateLimiter,
	IAppConfiguration configuration,
	ITelegramBotClient botClient,
	ILogger<ValidateTelegramRateLimitFilter> logger) : IAsyncActionFilter
{
	public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		if (!TryGetTelegramUpdate(context.ActionArguments, out var update))
		{
			await next();
			return;
		}

		var userId = update.Message!.Chat!.Id;

		if (rateLimiter.TryIncrement(userId))
		{
			await next();
			return;
		}

		logger.LogInformation(
			"Telegram update {UpdateId} rejected: user {UserId} exceeded rate limit of {Limit} requests per {Period}.",
			update.Id,
			userId,
			configuration.AiRequestLimitPerUser,
			configuration.AiRequestLimitPeriod);

		await botClient.SendMessage(
			userId,
			$"You have reached the limit of {configuration.AiRequestLimitPerUser} requests per {configuration.AiRequestLimitPeriod.TotalHours:0} hour(s). Please try again later.",
			cancellationToken: context.HttpContext.RequestAborted);

		context.Result = new OkResult();
	}

	private static bool TryGetTelegramUpdate(IDictionary<string, object?> actionArguments, out Update update)
	{
		if (actionArguments.TryGetValue("update", out var candidate) && candidate is Update typedUpdate)
		{
			update = typedUpdate;
			return true;
		}

		update = default!;
		return false;
	}
}
