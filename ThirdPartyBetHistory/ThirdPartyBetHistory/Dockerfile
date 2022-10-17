FROM mcr.microsoft.com/dotnet/sdk:6.0 as builder

COPY . .
WORKDIR ThirdPartyBetHistory/ThirdPartyBetHistory
RUN dotnet publish "ThirdPartyBetHistory.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=builder /app .
EXPOSE 59168

ENTRYPOINT ["dotnet", "ThirdPartyBetHistory.dll"]