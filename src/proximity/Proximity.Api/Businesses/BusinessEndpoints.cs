using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver;
using Shared.Mongo.Repositories;

namespace Proximity.Api.Businesses;

public static class BusinessEndpoints {
    private const double EarthRadiusKm = 6371.0;

    public static IEndpointRouteBuilder MapBusinessEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/api/businesses");

        group.MapPost("", async (CreateBusinessRequest request, IMongoRepository<Business, string> repository) => {
            var existing = await repository.GetAsync(request.BusinessId);
            if (existing is not null) {
                return Results.Conflict(new { message = "Business already exists." });
            }

            var business = new Business {
                Id = request.BusinessId,
                Address = request.Address,
                City = request.City,
                State = request.State,
                Country = request.Country,
                Location = toPoint(request.Latitude, request.Longtitude)
            };

            await repository.AddAsync(business);
            return Results.Created($"/api/businesses/{business.Id}", toResponse(business));
        })
            .WithName("CreateBusiness");

        group.MapGet("{businessId}", async (string businessId, IMongoRepository<Business, string> repository) => {
            var business = await repository.GetAsync(businessId);
            return business is null ? Results.NotFound() : Results.Ok(toResponse(business));
        })
            .WithName("GetBusiness");

        group.MapPut("{businessId}", async (string businessId, UpdateBusinessRequest request, IMongoRepository<Business, string> repository) => {
            var existing = await repository.GetAsync(businessId);
            if (existing is null) {
                return Results.NotFound();
            }

            var updated = existing with {
                Address = request.Address,
                City = request.City,
                State = request.State,
                Country = request.Country,
                Location = toPoint(request.Latitude, request.Longtitude)
            };

            await repository.UpdateAsync(updated);
            return Results.Ok(toResponse(updated));
        })
            .WithName("UpdateBusiness");

        group.MapDelete("{businessId}", async (string businessId, IMongoRepository<Business, string> repository) => {
            var existing = await repository.GetAsync(businessId);
            if (existing is null) {
                return Results.NotFound();
            }

            await repository.DeleteAsync(businessId);
            return Results.NoContent();
        })
            .WithName("DeleteBusiness");

        return endpoints;
    }

    public static IEndpointRouteBuilder MapBusinessSearch(this IEndpointRouteBuilder endpoints) {
        endpoints.MapGet("/api/search", async (
                double latitude,
                double longtitude,
                string radius,
                IMongoRepository<Business, string> repository) => {
                    if (!tryGetRadiusKm(radius, out var radiusKm)) {
                        return Results.BadRequest(new { message = "Radius must be one of: 0.5km, 1km, 2km, 5km, 20km." });
                    }

                    var radiusRadians = radiusKm / EarthRadiusKm;
                    // Geohash
                    var geoFilter = Builders<Business>.Filter.GeoWithinCenterSphere(
                        b => b.Location,
                        longtitude,
                        latitude,
                        radiusRadians
                    );
                    var businesses = await repository.Collection.Find(geoFilter).ToListAsync();
                    var response = businesses.Select(toResponse).ToList();
                    return Results.Ok(response);
                })
            .WithName("SearchBusinesses");

        return endpoints;
    }

    private static BusinessResponse toResponse(Business business) => new(
        business.Id,
        business.Address,
        business.City,
        business.State,
        business.Country,
        business.Location?.Coordinates.Latitude ?? 0,
        business.Location?.Coordinates.Longitude ?? 0
    );

    private static GeoJsonPoint<GeoJson2DGeographicCoordinates> toPoint(double latitude, double longitude)
        => new(new GeoJson2DGeographicCoordinates(longitude, latitude));

    private static bool tryGetRadiusKm(string radius, out double radiusKm) {
        radiusKm = radius.Trim().ToLowerInvariant() switch {
            "0.5km" => 0.5,
            "1km" => 1,
            "2km" => 2,
            "5km" => 5,
            "20km" => 20,
            _ => 0
        };

        return radiusKm > 0;
    }
}
