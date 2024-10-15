FROM hub.lumigame.com/base/dotnet/core/sdk:6.0 AS builder
ARG CONFIGURATION=Release
COPY ./ThirdPartyService ./ThirdPartyService
WORKDIR ThirdPartyService
RUN dotnet restore

RUN dotnet build -c $CONFIGURATION -o /app

FROM builder AS publish
ARG CONFIGURATION=Release
RUN dotnet publish -c $CONFIGURATION -o /app

FROM hub.lumigame.com/base/dotnet/core/sdk:6.0
WORKDIR /app
COPY --from=publish /app .
EXPOSE 5000

ENTRYPOINT ["dotnet", "ThirdPartyService.dll"]