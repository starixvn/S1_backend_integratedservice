FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

COPY . .
WORKDIR ThirdPartyWager/ThirdPartyWager
RUN dotnet publish "ThirdPartyWager.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=builder /app .
EXPOSE 8888

ENTRYPOINT ["dotnet", "ThirdPartyWager.dll"]