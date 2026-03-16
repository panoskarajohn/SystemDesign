using GoogleMaps.Api.Locations;
using GoogleMaps.Api.Directions;
using GoogleMaps.Api.Geocoding;
using GoogleMaps.Api.Geocoding.Mongo;
using GoogleMaps.Api.Geocoding.Services;
using GoogleMaps.Api.TileCaching;
using Shared.Mongo;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddMongo(configuration);
services.AddMongoRepository<UserLocation, string>("user_locations");
services.AddMongoRepository<GeoLocationDocument, string>("geo_locations");
services.AddTransient<IGeoLocationRepository, GeoLocationRepository>();
services.AddTransient<GeocodingService>();
services.AddHttpClient<IMapTileCacheService, MapTileCacheService>();
services.AddCors(options => {
    options.AddPolicy("FrontendLocal", policy => {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("FrontendLocal");

app.MapGet("/api/health", () => "healthy");
app.MapLocationEndpoints();
app.MapGeocodingEndpoints();
app.MapDirectionsEndpoints();
app.MapTileEndpoints();

app.Run();

public partial class Program {
}
