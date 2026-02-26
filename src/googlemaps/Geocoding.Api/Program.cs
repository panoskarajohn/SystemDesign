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

        var id = await geocodingService.InsertGeocodingResultAsync(request, cancellationToken);
        return Results.Accepted(value: new InsertGeolocationResponse(id));
    });

app.MapGet("/v1/geolocations/{id}", async (
    string id,
    GeocodingService geocodingService,
    CancellationToken cancellationToken) => {
        var document = await geocodingService.GetByIdAsync(id, cancellationToken);
        if (document is null) {
            return Results.NotFound();
        }

        var response = new GeolocationResponse(
            document.Id,
            document.Input,
            document.Language,
            document.RegionBias,
            document.Source,
            document.PlusCode,
            new GeolocationPointResponse(
                document.Location.Coordinates.Longitude,
                document.Location.Coordinates.Latitude
            ),
            document.Timestamp
        );

        return Results.Ok(response);
    });

app.MapDelete("/v1/geolocations/{id}", async (
    string id,
    GeocodingService geocodingService,
    CancellationToken cancellationToken) => {
        var deleted = await geocodingService.DeleteByIdAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    });

app.Run();

public partial class Program {
}
