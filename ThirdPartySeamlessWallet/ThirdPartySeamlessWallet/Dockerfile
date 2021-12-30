FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

COPY . .
WORKDIR ThirdPartySeamlessWallet/ThirdPartySeamlessWallet
RUN dotnet publish "ThirdPartySeamlessWallet.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=builder /app .
EXPOSE 59259

ENTRYPOINT ["dotnet", "ThirdPartySeamlessWallet.dll"]