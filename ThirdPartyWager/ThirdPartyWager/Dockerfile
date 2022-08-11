FROM mcr.microsoft.com/dotnet/sdk:6.0 as builder

COPY . .
WORKDIR ThirdPartyWager/ThirdPartyWager
RUN dotnet restore
RUN dotnet publish "ThirdPartyWager.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=builder /app .
EXPOSE 8888

ENTRYPOINT ["dotnet", "ThirdPartyWager.dll"]