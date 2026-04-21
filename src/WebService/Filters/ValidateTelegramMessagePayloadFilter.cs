using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace WebService.Filters;

internal sealed class ValidateTelegramMessagePayloadFilter : IAsyncActionFilter
{
	public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		if (!TryGetTelegramUpdate(context.ActionArguments, out var update) ||
			update.Type != UpdateType.Message ||
			update.Message?.Text is not { Length: > 0 } ||
			update.Message.Chat is null)
		{
			context.Result = new OkResult();
			return Task.CompletedTask;
		}

		return next();
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