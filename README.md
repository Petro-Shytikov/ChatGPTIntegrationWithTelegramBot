# ChatGPTIntegrationWithTelegramBot

ASP.NET Core Web API that receives Telegram bot updates and forwards user messages to ChatGPT using Microsoft Agent Framework with OpenAI.

## What this service does

- Exposes a Telegram webhook endpoint at /telegram/webhook.
- Validates Telegram secret header on every webhook call.
- Rejects oversized Telegram messages using an MVC filter and returns a rejection reason.
- Sends incoming text messages to ChatGPT.
- Sends ChatGPT response back to the same Telegram chat.
- Exposes /health endpoint for CI and runtime health checks.
- Registers Telegram webhook automatically on application startup.

## Tech stack

- .NET 10
- Telegram.Bot
- Microsoft.Agents.AI.OpenAI
- OpenAI SDK for .NET

## Configuration sources

Read from environment variables only:

- TELEGRAM_BOT_TOKEN (required)
- TELEGRAM_PUBLIC_WEBHOOK_URL (required)
- TELEGRAM_WEBHOOK_SECRET_TOKEN (required)
- OPENAI_API_KEY (required)

Read from appsettings only:

- BotSettings:ChatGptModel (required)
- BotSettings:ChatGptSystemPrompt (required)
- BotSettings:RetryTelegramWebhookInitializerDelay (required TimeSpan, for example 00:00:30)
- BotSettings:MaxTelegramRequestLength (required integer, for example 1500)
- BotSettings:AiRequestLimitPerUser (required integer, for example 10)
- BotSettings:AiRequestLimitPeriod (required TimeSpan, for example 24:00:00)

## Telegram request filtering

- The webhook endpoint applies a message-length filter before forwarding text to ChatGPT.
- If the incoming message length exceeds `BotSettings:MaxTelegramRequestLength`, the request is rejected.
- The bot sends a response explaining the reason for rejection and includes the configured limit.

## AI request rate limiting

- The webhook endpoint applies a per-user AI request limit filter before forwarding text to ChatGPT.
- Per-user request count is controlled by `BotSettings:AiRequestLimitPerUser`.
- Block duration is controlled by `BotSettings:AiRequestLimitPeriod`.
- When a user reaches the configured request limit, further requests are rejected until the configured period expires.
- Rate limit state is stored in in-memory cache and automatically removed for inactive users using sliding expiration.

## Local run (without Docker)

1. Set environment variables in your shell.
2. Run the service:

```bash
dotnet run --project src/WebService/WebService.csproj
```

3. Verify health endpoint:

```bash
curl http://localhost:5000/health
```

Note: local port can vary by launch profile. In Docker we use port 8080.

## Docker run

1. Create .env based on .env.example and fill your real values.
2. Build image:

```bash
docker build -t chatgptintegrationwithtelegrambot-env .
```

3. Run container:

```bash
docker run --rm --env-file .env -p 8080:8080 chatgptintegrationwithtelegrambot-env
```

4. Verify health endpoint:

```bash
curl http://localhost:8080/health
```

## Telegram webhook registration

On startup, the service automatically registers webhook URL in Telegram using `TELEGRAM_PUBLIC_WEBHOOK_URL`.

Configured webhook value:

- {TELEGRAM_PUBLIC_WEBHOOK_URL}/telegram/webhook

The URL must be publicly reachable by Telegram.
If registration fails, a background service retries every `BotSettings:RetryTelegramWebhookInitializerDelay` until it succeeds, then stops retrying.

## Endpoints

- GET /
- GET /health
- POST /telegram/webhook

## Security notes

- Never commit real bot tokens or API keys.
- Keep secrets in environment variables or secret stores.
- If a token or API key was committed or shared accidentally, rotate it immediately.
