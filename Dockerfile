FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/WebService/WebService.csproj", "src/WebService/"]
RUN dotnet restore "src/WebService/WebService.csproj"

COPY . .
RUN dotnet publish "src/WebService/WebService.csproj" --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Required runtime environment variables:
# - TELEGRAM_BOT_TOKEN
# - TELEGRAM_PUBLIC_WEBHOOK_URL
# - OPENAI_API_KEY
# Optional runtime environment variables:
# - TELEGRAM_WEBHOOK_SECRET_TOKEN
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebService.dll"]