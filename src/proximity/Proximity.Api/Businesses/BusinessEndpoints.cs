using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Mongo.Repositories;

namespace Proximity.Api.Businesses;

public static class BusinessEndpoints {
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
                    Latitude = request.Latitude,
                    Longtitude = request.Longtitude
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
                    Latitude = request.Latitude,
                    Longtitude = request.Longtitude
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

    private static BusinessResponse toResponse(Business business) => new(
        business.Id,
        business.Address,
        business.City,
        business.State,
        business.Country,
        business.Latitude,
        business.Longtitude
    );
}
