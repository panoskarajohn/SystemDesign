FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/googlemaps/GoogleMaps.Api/GoogleMaps.Api.csproj", "src/googlemaps/GoogleMaps.Api/"]
COPY ["src/shared/Shared.Mongo/Shared.Mongo.csproj", "src/shared/Shared.Mongo/"]

RUN dotnet restore "src/googlemaps/GoogleMaps.Api/GoogleMaps.Api.csproj"

COPY . .

WORKDIR "/src/src/googlemaps/GoogleMaps.Api"
RUN dotnet build "GoogleMaps.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GoogleMaps.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GoogleMaps.Api.dll"]
