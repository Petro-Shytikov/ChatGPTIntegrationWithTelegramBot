using System.Text;
using Telegram.Bot.Types;
using WebService.Services;

namespace WebService.Helpers;

public static class TelegramBotHelper
{
	public static TelegramBotCommandType TryHandle(Update update)
	{
		var messageText = update.Message?.Text;
		if (string.IsNullOrWhiteSpace(messageText))
			return TelegramBotCommandType.None;

		if (!messageText.StartsWith('/'))
			return TelegramBotCommandType.None;

		var command = messageText.Split(' ', 2)[0].ToLowerInvariant();

		return command switch
		{
			"/ask" => TelegramBotCommandType.Ask,
			"/info" => TelegramBotCommandType.Info,
			_ => TelegramBotCommandType.Unknown
		};
	}

	public static string BuildInfoReply(UserRateLimitStatus status, IAppConfiguration configuration)
	{
		string requestStatus;
		if (status.BlockedUntil is { } blockedUntil)
		{
			requestStatus = $"0 of {configuration.AiRequestLimitPerUser} (blocked until {blockedUntil:yyyy-MM-dd HH:mm} UTC)";
		}
		else
		{
			requestStatus = $"{status.RemainingRequests} of {configuration.AiRequestLimitPerUser}";
		}

		return new StringBuilder()
			.AppendLine($"Service Version: {CommonHelper.ServiceVersion}")
			.AppendLine($"AI Model: {configuration.ChatGptModel}")
			.AppendLine($"Max Request Length: {configuration.MaxTelegramRequestLength} characters")
			.AppendLine($"Available Requests: {requestStatus}")
			.Append($"Request Limit Period: {configuration.AiRequestLimitPeriod.TotalHours:0} hour(s)")
			.ToString();
	}
}