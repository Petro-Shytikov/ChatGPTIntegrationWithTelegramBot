using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace WebService.Filters;

internal sealed class ValidateTelegramMessageLengthFilter(
	IAppConfiguration configuration,
	ITelegramBotClient botClient,
	ILogger<ValidateTelegramMessageLengthFilter> logger) : IAsyncActionFilter
{
	public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		if (!TryGetTelegramUpdate(context.ActionArguments, out var update))
		{
			await next();
			return;
		}

		var userText = update.Message!.Text!;

		if (userText.Length <= configuration.MaxTelegramRequestLength)
		{
			await next();
			return;
		}

		logger.LogInformation(
			"Telegram update {UpdateId} rejected due to message length {MessageLength} over configured limit {MaxLength}.",
			update.Id,
			userText.Length,
			configuration.MaxTelegramRequestLength);

		await botClient.SendMessage(
			update.Message!.Chat!.Id,
			$"Your request was rejected because it exceeds the maximum allowed length of {configuration.MaxTelegramRequestLength} characters.",
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