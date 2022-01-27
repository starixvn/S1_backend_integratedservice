FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

COPY . .
WORKDIR ThirdPartyBetHistory/ThirdPartyBetHistory
RUN dotnet publish "ThirdPartyBetHistory.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=builder /app .
EXPOSE 59168

ENTRYPOINT ["dotnet", "ThirdPartyBetHistory.dll"]