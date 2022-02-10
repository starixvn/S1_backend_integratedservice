FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

COPY . .
WORKDIR ThirdPartySeamlessWallet/ThirdPartySettleWorker
RUN dotnet publish "ThirdPartySettleWorker.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=builder /app .
EXPOSE 80

ENTRYPOINT ["dotnet", "ThirdPartySettleWorker.dll"]