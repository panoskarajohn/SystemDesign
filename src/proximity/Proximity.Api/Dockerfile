FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files for restore (keeping relative paths consistent)
COPY ["src/proximity/Proximity.Api/Proximity.Api.csproj", "src/proximity/Proximity.Api/"]
COPY ["src/shared/Shared.Common/Shared.Common.csproj", "src/shared/Shared.Common/"]
COPY ["src/shared/Shared.Logging/Shared.Logging.csproj", "src/shared/Shared.Logging/"]
COPY ["src/shared/Shared.Web/Shared.Web.csproj", "src/shared/Shared.Web/"]
COPY ["src/shared/Shared.Mongo/Shared.Mongo.csproj", "src/shared/Shared.Mongo/"]

RUN dotnet restore "src/proximity/Proximity.Api/Proximity.Api.csproj"

# Copy all source code
COPY . .

# Build from correct path
WORKDIR "/src/src/proximity/Proximity.Api"
RUN dotnet build "Proximity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Proximity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Proximity.Api.dll"]