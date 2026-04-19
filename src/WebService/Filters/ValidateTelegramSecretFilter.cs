using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebService.Filters;

internal sealed class ValidateTelegramSecretFilter(IAppConfiguration configuration) : IAsyncActionFilter
{
	public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		if (!IsValidTelegramSecret(context.HttpContext.Request, configuration.TelegramWebhookSecretToken))
		{
			context.Result = new UnauthorizedResult();
			return Task.CompletedTask;
		}

		return next();
	}

	private static bool IsValidTelegramSecret(HttpRequest request, string expectedSecretToken)
	{
		if (string.IsNullOrWhiteSpace(expectedSecretToken))
		{
			return false;
		}

		if (!request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var providedSecretToken))
		{
			return false;
		}

		var providedSecretTokenValue = providedSecretToken.ToString();
		if (string.IsNullOrWhiteSpace(providedSecretTokenValue))
		{
			return false;
		}

		return string.Equals(providedSecretTokenValue, expectedSecretToken, StringComparison.Ordinal);
	}
}
