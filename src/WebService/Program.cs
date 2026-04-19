using WebService.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ValidateTelegramSecretFilter>();
builder.Services.AddHealthChecks();
builder.Services.AddAppConfiguration();
builder.Services.AddTelegramBotClient();
builder.Services.AddChatGptAgent();
builder.Services.AddTelegramWebhookInitialization();

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
