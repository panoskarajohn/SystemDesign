using GoogleMaps.Api.Geocoding.Contracts;
using GoogleMaps.Api.Geocoding.Services;

namespace GoogleMaps.Api.Geocoding;

public static class GeocodingEndpoints {
    public static IEndpointRouteBuilder MapGeocodingEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapPost("/v1/geolocations", async (
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

        endpoints.MapGet("/v1/geolocations/{id}", async (
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

        endpoints.MapDelete("/v1/geolocations/{id}", async (
            string id,
            GeocodingService geocodingService,
            CancellationToken cancellationToken) => {
            var deleted = await geocodingService.DeleteByIdAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return endpoints;
    }
}
