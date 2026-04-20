using Telegram.Bot;

internal sealed class TelegramWebhookInitializerBackgroundService(
	ITelegramBotClient botClient,
	IAppConfiguration configuration,
	ILogger<TelegramWebhookInitializerBackgroundService> logger) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (string.IsNullOrWhiteSpace(configuration.TelegramPublicWebhookUrl))
		{
			throw new InvalidOperationException("TELEGRAM_PUBLIC_WEBHOOK_URL is required for webhook registration.");
		}

		if (configuration.RetryTelegramWebhookInitializerDelay <= TimeSpan.Zero)
		{
			throw new InvalidOperationException("BotSettings:RetryTelegramWebhookInitializerDelay must be greater than zero.");
		}

		var webhookUrl = new Uri(new Uri(configuration.TelegramPublicWebhookUrl), "/telegram/webhook").ToString();
		var retryInterval = configuration.RetryTelegramWebhookInitializerDelay;

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await botClient.SetWebhook(
					webhookUrl,
					secretToken: configuration.TelegramWebhookSecretToken,
					cancellationToken: stoppingToken);

				logger.LogInformation("Telegram webhook configured at {WebhookUrl}", webhookUrl);
				return;
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				return;
			}
			catch (Exception exception)
			{
				logger.LogWarning(
					exception,
					"Failed to configure Telegram webhook. Retrying in {RetryIntervalSeconds} seconds.",
					retryInterval.TotalSeconds);
			}

			try
			{
				await Task.Delay(retryInterval, stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				return;
			}
		}
	}
}