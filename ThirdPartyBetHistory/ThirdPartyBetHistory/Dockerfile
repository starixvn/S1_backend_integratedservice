FROM hub.lumigame.com/base/dotnet/core/sdk:6.0 as builder

COPY . .
WORKDIR ThirdPartyBetHistory/ThirdPartyBetHistory
RUN dotnet publish "ThirdPartyBetHistory.csproj" -c Release -o /app

FROM hub.lumigame.com/base/dotnet/core/sdk:6.0
WORKDIR /app
COPY --from=builder /app .
EXPOSE 5000

ENTRYPOINT ["dotnet", "ThirdPartyBetHistory.dll"]