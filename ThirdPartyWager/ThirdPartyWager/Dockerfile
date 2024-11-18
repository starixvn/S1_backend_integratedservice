FROM hub.nexdev.net/base/dotnet/core/sdk:6.0 as builder

COPY . .
WORKDIR ThirdPartyWager/ThirdPartyWager
RUN dotnet restore
RUN dotnet publish "ThirdPartyWager.csproj" -c Release -o /app

FROM hub.nexdev.net/base/dotnet/core/aspnet:6.0-alpine
WORKDIR /app
COPY --from=builder /app .
EXPOSE 5000

ENTRYPOINT ["dotnet", "ThirdPartyWager.dll"]