FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS builder
ARG CONFIGURATION=Release
COPY ./CommonService ./CommonService
WORKDIR CommonService
RUN dotnet restore

RUN dotnet build -c $CONFIGURATION -o /app

FROM builder AS publish
ARG CONFIGURATION=Release
RUN dotnet publish -c $CONFIGURATION -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=publish /app .
EXPOSE 59359

ENTRYPOINT ["dotnet", "CommonService.dll"]