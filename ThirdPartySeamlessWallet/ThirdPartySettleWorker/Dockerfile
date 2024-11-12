FROM hub.lumigame.com/base/dotnet/core/sdk:6.0 as builder

COPY . .
WORKDIR ThirdPartySeamlessWallet/ThirdPartySettleWorker
RUN dotnet publish "ThirdPartySettleWorker.csproj" -c Release -o /app

FROM hub.lumigame.com/base/dotnet/core/sdk:6.0
WORKDIR /app
COPY --from=builder /app .
EXPOSE 5000

ENTRYPOINT ["dotnet", "ThirdPartySettleWorker.dll"]