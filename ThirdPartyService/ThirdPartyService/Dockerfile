FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder
ARG CONFIGURATION=Release
COPY ./ThirdPartyService ./ThirdPartyService
WORKDIR ThirdPartyService
RUN dotnet restore

RUN dotnet build -c $CONFIGURATION -o /app

FROM builder AS publish
ARG CONFIGURATION=Release
RUN dotnet publish -c $CONFIGURATION -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app
COPY --from=publish /app .
EXPOSE 55996

ENTRYPOINT ["dotnet", "ThirdPartyService.dll"]