FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 59259

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["ThirdPartySeamlessWallet/ThirdPartySeamlessWallet/ThirdPartySeamlessWallet.csproj", "ThirdPartySeamlessWallet/"]
RUN dotnet restore "ThirdPartySeamlessWallet/ThirdPartySeamlessWallet.csproj"
COPY . .
WORKDIR "/src/ThirdPartySeamlessWallet"
RUN dotnet build "ThirdPartySeamlessWallet.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ThirdPartySeamlessWallet.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ThirdPartySeamlessWallet.dll"]