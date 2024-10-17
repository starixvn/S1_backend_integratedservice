FROM hub.lumigame.com/base/dotnet/core/sdk:6.0 as builder

COPY . .
WORKDIR ThirdPartyWager/ThirdPartyWager
RUN dotnet restore
RUN dotnet publish "ThirdPartyWager.csproj" -c Release -o /app

FROM hub.lumigame.com/base/dotnet/core/sdk:6.0
WORKDIR /app
COPY --from=builder /app .
EXPOSE 5000

ENTRYPOINT ["dotnet", "ThirdPartyWager.dll"]