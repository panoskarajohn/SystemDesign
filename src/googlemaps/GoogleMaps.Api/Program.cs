using GoogleMaps.Api.Locations;
using GoogleMaps.Api.Geocoding;
using GoogleMaps.Api.Geocoding.Mongo;
using GoogleMaps.Api.Geocoding.Services;
using Shared.Mongo;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddMongo(configuration);
services.AddMongoRepository<UserLocation, string>("user_locations");
services.AddMongoRepository<GeoLocationDocument, string>("geo_locations");
services.AddTransient<IGeoLocationRepository, GeoLocationRepository>();
services.AddTransient<GeocodingService>();

var app = builder.Build();

app.MapGet("/api/health", () => "healthy");
app.MapLocationEndpoints();
app.MapGeocodingEndpoints();

app.Run();

public partial class Program {
}
