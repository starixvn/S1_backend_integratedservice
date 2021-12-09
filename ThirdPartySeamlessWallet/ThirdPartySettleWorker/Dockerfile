#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ThirdPartySettleWorker/ThirdPartySettleWorker.csproj", "ThirdPartySettleWorker/"]
COPY ["ThirdPartySeamlessWallet/ThirdPartySeamlessWallet.csproj", "ThirdPartySeamlessWallet/"]
RUN dotnet restore "ThirdPartySettleWorker/ThirdPartySettleWorker.csproj"
COPY . .
WORKDIR "/src/ThirdPartySettleWorker"
RUN dotnet build "ThirdPartySettleWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ThirdPartySettleWorker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ThirdPartySettleWorker.dll"]