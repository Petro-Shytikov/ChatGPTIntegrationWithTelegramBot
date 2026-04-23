using WebService.Filters;
using WebService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ValidateTelegramSecretFilter>();
builder.Services.AddSingleton<ValidateTelegramMessagePayloadFilter>();
builder.Services.AddSingleton<ValidateTelegramMessageLengthFilter>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IUserRequestRateLimiter, UserRequestRateLimiter>();
builder.Services.AddSingleton<ValidateTelegramRateLimitFilter>();
builder.Services.AddHealthChecks();
builder.Services.AddAppConfiguration();
builder.Services.AddTelegramBotClient();
builder.Services.AddChatGptAgent();
builder.Services.AddTelegramWebhookInitialization();

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
