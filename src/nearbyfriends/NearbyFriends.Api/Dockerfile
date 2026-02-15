FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/nearbyfriends/NearbyFriends.Api/NearbyFriends.Api.csproj", "src/nearbyfriends/NearbyFriends.Api/"]
COPY ["src/shared/Shared.Postgres/Shared.Postgres.csproj", "src/shared/Shared.Postgres/"]
COPY ["src/shared/Shared.Common/Shared.Common.csproj", "src/shared/Shared.Common/"]

RUN dotnet restore "src/nearbyfriends/NearbyFriends.Api/NearbyFriends.Api.csproj"

COPY . .

WORKDIR "/src/src/nearbyfriends/NearbyFriends.Api"
RUN dotnet build "NearbyFriends.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "NearbyFriends.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NearbyFriends.Api.dll"]
