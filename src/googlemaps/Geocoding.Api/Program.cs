using Geocoding.Api.Mongo;
using Geocoding.Api.Geocoding;
using Geocoding.Api.Contracts;
using Shared.Mongo;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddMongo(configuration);
services.AddMongoRepository<GeoLocationDocument, string>("geo_locations");
services.AddTransient<IGeoLocationRepository, GeoLocationRepository>();
services.AddTransient<GeocodingService>();

var app = builder.Build();

app.MapGet("/api/health", () => "healthy");

app.MapPost("/v1/geolocations", async (
    InsertGeolocationRequest request,
    GeocodingService geocodingService,
    CancellationToken cancellationToken) => {
        if (string.IsNullOrWhiteSpace(request.Input)) {
            return Results.BadRequest(new { message = "input is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Language)
            || string.IsNullOrWhiteSpace(request.RegionBias)
            || string.IsNullOrWhiteSpace(request.Source)) {
            return Results.BadRequest(new { message = "language, region_bias and source are required." });
        }

        await geocodingService.InsertGeocodingResultAsync(request, cancellationToken);
        return Results.Accepted();
    });

app.Run();

public partial class Program {
}
