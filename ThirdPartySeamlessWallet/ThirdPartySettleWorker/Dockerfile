FROM mcr.microsoft.com/dotnet/sdk:6.0 as builder

COPY . .
WORKDIR ThirdPartySeamlessWallet/ThirdPartySettleWorker
RUN dotnet publish "ThirdPartySettleWorker.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=builder /app .
EXPOSE 80

ENTRYPOINT ["dotnet", "ThirdPartySettleWorker.dll"]