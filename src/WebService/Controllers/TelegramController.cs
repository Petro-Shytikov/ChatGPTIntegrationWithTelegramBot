using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WebService.Filters;

namespace WebService.Controllers;

[ApiController]
[Route("telegram")]
public sealed class TelegramController : ControllerBase
{
	private readonly ITelegramBotClient _botClient;
	private readonly AIAgent _aiAgent;
	private readonly ILogger<TelegramController> _logger;

	public TelegramController(
		ITelegramBotClient botClient,
		AIAgent aiAgent,
		ILogger<TelegramController> logger)
	{
		_botClient = botClient;
		_aiAgent = aiAgent;
		_logger = logger;
	}

	[ServiceFilter(typeof(ValidateTelegramSecretFilter))]
	[HttpPost("webhook")]
	public async Task<IActionResult> Webhook([FromBody] Update update, CancellationToken cancellationToken)
	{
		if (update.Type != UpdateType.Message || update.Message?.Text is not { Length: > 0 } userText)
		{
			return Ok();
		}

		if (update.Message.Chat is null)
		{
			return Ok();
		}

		try
		{
			var aiResponse = await _aiAgent.RunAsync(userText, cancellationToken: cancellationToken);
			var responseText = aiResponse?.ToString();
			if (string.IsNullOrWhiteSpace(responseText))
			{
				responseText = "I could not generate a response right now.";
			}

			await _botClient.SendMessage(update.Message.Chat.Id, responseText, cancellationToken: cancellationToken);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Failed to process Telegram update {UpdateId}", update.Id);
			await _botClient.SendMessage(
				update.Message.Chat.Id,
				"Sorry, I could not process your request right now.",
				cancellationToken: cancellationToken);
		}

		return Ok();
	}
}
