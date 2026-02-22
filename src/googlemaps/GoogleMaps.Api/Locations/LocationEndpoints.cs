using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using Shared.Mongo.Repositories;

namespace GoogleMaps.Api.Locations;

public static class LocationEndpoints {
    private const int BatchWindowSeconds = 15;

    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapPost("/v1/locations", async (
            PostLocationsRequest request,
            IMongoRepository<UserLocation, string> repository) => {
            if (string.IsNullOrWhiteSpace(request.UserId)) {
                return Results.BadRequest(new { message = "user_id is required." });
            }

            if (request.Locations is null || request.Locations.Count == 0) {
                return Results.BadRequest(new { message = "At least one location is required." });
            }

            var invalidPoint = request.Locations.FirstOrDefault(p =>
                p.Latitude is < -90 or > 90 ||
                p.Longitude is < -180 or > 180);

            if (invalidPoint is not null) {
                return Results.BadRequest(new { message = "Latitude/Longitude values are out of range." });
            }

            var documents = request.Locations.Select(p => new UserLocation {
                UserId = request.UserId.Trim(),
                Location = toPoint(p.Latitude, p.Longitude),
                Timestamp = p.Timestamp,
                BatchWindowSeconds = BatchWindowSeconds
            }).ToList();

            await repository.Collection.InsertManyAsync(documents);

            return Results.Ok(new PostLocationsResponse(request.UserId.Trim(), documents.Count));
        });

        endpoints.MapGet("/v1/locations/{userId}/last", async (
            string userId,
            IMongoRepository<UserLocation, string> repository) => {
            if (string.IsNullOrWhiteSpace(userId)) {
                return Results.BadRequest(new { message = "userId is required." });
            }

            var latest = await repository.Collection
                .Find(x => x.UserId == userId)
                .SortByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync();

            if (latest is null) {
                return Results.NotFound();
            }

            return Results.Ok(new LastUserLocationResponse(
                latest.UserId,
                latest.Location.Coordinates.Latitude,
                latest.Location.Coordinates.Longitude,
                latest.Timestamp
            ));
        });

        return endpoints;
    }

    private static GeoJsonPoint<GeoJson2DGeographicCoordinates> toPoint(double latitude, double longitude)
        => new(new GeoJson2DGeographicCoordinates(longitude, latitude));
}
