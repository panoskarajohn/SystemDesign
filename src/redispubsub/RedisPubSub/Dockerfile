FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/redispubsub/RedisPubSub/RedisPubSub.csproj", "src/redispubsub/RedisPubSub/"]
COPY ["src/shared/Shared.Redis/Shared.Redis.csproj", "src/shared/Shared.Redis/"]

RUN dotnet restore "src/redispubsub/RedisPubSub/RedisPubSub.csproj"

COPY . .

WORKDIR "/src/src/redispubsub/RedisPubSub"
RUN dotnet build "RedisPubSub.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RedisPubSub.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedisPubSub.dll"]
