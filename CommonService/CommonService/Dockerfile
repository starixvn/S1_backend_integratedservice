FROM hub.nexdev.net/base/dotnet/core/sdk:6.0 AS builder

COPY . .
WORKDIR CommonService/CommonService
RUN dotnet publish "CommonService.csproj" -c Release -o /app

FROM hub.nexdev.net/base/dotnet/core/aspnet:6.0-alpine
WORKDIR /app
COPY --from=builder /app .
EXPOSE 5000

ENTRYPOINT ["dotnet", "CommonService.dll"]