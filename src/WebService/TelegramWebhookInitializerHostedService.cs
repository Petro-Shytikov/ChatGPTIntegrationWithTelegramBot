using Telegram.Bot;

internal sealed class TelegramWebhookInitializerHostedService(
	ITelegramBotClient botClient,
	IAppConfiguration configuration,
	ILogger<TelegramWebhookInitializerHostedService> logger) : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(configuration.TelegramPublicWebhookUrl))
		{
			throw new InvalidOperationException("TELEGRAM_PUBLIC_WEBHOOK_URL is required for webhook registration.");
		}

		var webhookUrl = configuration.TelegramPublicWebhookUrl.TrimEnd('/') + "/telegram/webhook";
		await botClient.SetWebhook(
			webhookUrl,
			secretToken: configuration.TelegramWebhookSecretToken,
			cancellationToken: cancellationToken);

		logger.LogInformation("Telegram webhook configured at {WebhookUrl}", webhookUrl);
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}