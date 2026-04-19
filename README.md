# ChatGPTIntegrationWithTelegramBot

ASP.NET Core Web API that receives Telegram bot updates and forwards user messages to ChatGPT using Microsoft Agent Framework with OpenAI.

## What this service does

- Exposes a Telegram webhook endpoint at /telegram/webhook.
- Validates Telegram secret header when TELEGRAM_WEBHOOK_SECRET_TOKEN is configured.
- Sends incoming text messages to ChatGPT.
- Sends ChatGPT response back to the same Telegram chat.
- Exposes /health endpoint for CI and runtime health checks.
- Exposes /telegram/set-webhook endpoint to register webhook in Telegram.

## Tech stack

- .NET 10
- Telegram.Bot
- Microsoft.Agents.AI.OpenAI
- OpenAI SDK for .NET

## Configuration sources

Read from environment variables only:

- TELEGRAM_BOT_TOKEN (required)
- TELEGRAM_PUBLIC_WEBHOOK_URL (required only for POST /telegram/set-webhook)
- TELEGRAM_WEBHOOK_SECRET_TOKEN (optional)
- OPENAI_API_KEY (required)

Read from appsettings only:

- BotSettings:ChatGptModel (default: gpt-5.4-mini)
- BotSettings:ChatGptSystemPrompt (optional)

Legacy appsettings path also supported:

- ChatGpt:Model
- ChatGpt:SystemPrompt

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

## Register Telegram webhook

After app is running and reachable from internet, call:

```bash
curl -X POST http://localhost:8080/telegram/set-webhook
```

This endpoint uses TELEGRAM_PUBLIC_WEBHOOK_URL and configures:

- {TELEGRAM_PUBLIC_WEBHOOK_URL}/telegram/webhook

## Endpoints

- GET /
- GET /health
- POST /telegram/webhook
- POST /telegram/set-webhook

## Security notes

- Never commit real bot tokens or API keys.
- Keep secrets in environment variables or secret stores.
- If a token or API key was committed or shared accidentally, rotate it immediately.

## Project structure

- src/WebService/Program.cs: app startup and DI configuration.
- src/WebService/Controllers/TelegramController.cs: Telegram webhook and webhook registration endpoints.
- src/WebService/Controllers/HomeController.cs: root status endpoint.
- src/WebService/Configuration.cs: strongly-typed configuration model.
- src/WebService/IAppConfiguration.cs: configuration interface used by services/controllers.
- src/WebService/ConfigurationProvider.cs: builds and validates configuration (env-only secrets + appsettings chat options).
- src/WebService/ServiceCollectionExtensions.cs: DI registration extensions.
- src/WebService/WebService.csproj: package references.
- Dockerfile: multi-stage container build and runtime image.
