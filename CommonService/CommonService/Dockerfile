FROM mcr.microsoft.com/dotnet/sdk:8.0 as builder
ARG CONFIGURATION=Release
COPY ./CommonService ./CommonService
WORKDIR CommonService
RUN dotnet restore

RUN dotnet build -c $CONFIGURATION -o /app

FROM builder AS publish
ARG CONFIGURATION=Release
RUN dotnet publish -c $CONFIGURATION -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app .
EXPOSE 59359

ENTRYPOINT ["dotnet", "CommonService.dll"]