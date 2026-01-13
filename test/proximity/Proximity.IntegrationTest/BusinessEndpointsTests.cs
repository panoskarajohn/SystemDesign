using System.Net;

namespace Proximity.IntegrationTest;

[Collection("ProximityTests")]
public class BusinessEndpointsTests {
    private readonly ProximityClient _proximityClient;

    public BusinessEndpointsTests(ProximityTestFixture fixture) {
        _proximityClient = fixture.ProximityClient;
    }

    [Fact]
    public async Task CreateAndGetBusinessShouldSucceed() {
        var businessId = $"biz-{Guid.NewGuid():N}";
        var request = new CreateBusinessRequest(
            businessId,
            "1 Main St",
            "Denver",
            "CO",
            "USA",
            39.7392,
            -104.9903
        );

        try {
            var createResponse = await _proximityClient.CreateBusinessAsync(request);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var getResponse = await _proximityClient.GetBusinessAsync(businessId);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var business = await _proximityClient.ReadBusinessAsync(getResponse);
            Assert.NotNull(business);
            Assert.Equal(request.BusinessId, business!.BusinessId);
            Assert.Equal(request.Address, business.Address);
            Assert.Equal(request.City, business.City);
            Assert.Equal(request.State, business.State);
            Assert.Equal(request.Country, business.Country);
            Assert.Equal(request.Latitude, business.Latitude);
            Assert.Equal(request.Longtitude, business.Longtitude);
        }
        finally {
            await _proximityClient.DeleteBusinessAsync(businessId);
        }
    }

    [Fact]
    public async Task UpdateBusinessShouldReturnUpdatedPayload() {
        var businessId = $"biz-{Guid.NewGuid():N}";
        var createRequest = new CreateBusinessRequest(
            businessId,
            "10 Lake Rd",
            "Austin",
            "TX",
            "USA",
            30.2672,
            -97.7431
        );

        try {
            var createResponse = await _proximityClient.CreateBusinessAsync(createRequest);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var updateRequest = new UpdateBusinessRequest(
                "11 Lake Rd",
                "Austin",
                "TX",
                "USA",
                30.2673,
                -97.7432
            );

            var updateResponse = await _proximityClient.UpdateBusinessAsync(businessId, updateRequest);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var updated = await _proximityClient.ReadBusinessAsync(updateResponse);
            Assert.NotNull(updated);
            Assert.Equal(businessId, updated!.BusinessId);
            Assert.Equal(updateRequest.Address, updated.Address);
            Assert.Equal(updateRequest.City, updated.City);
            Assert.Equal(updateRequest.State, updated.State);
            Assert.Equal(updateRequest.Country, updated.Country);
            Assert.Equal(updateRequest.Latitude, updated.Latitude);
            Assert.Equal(updateRequest.Longtitude, updated.Longtitude);
        }
        finally {
            await _proximityClient.DeleteBusinessAsync(businessId);
        }
    }

    [Fact]
    public async Task DeleteBusinessShouldRemoveRecord() {
        var businessId = $"biz-{Guid.NewGuid():N}";
        var request = new CreateBusinessRequest(
            businessId,
            "55 Market St",
            "San Francisco",
            "CA",
            "USA",
            37.7749,
            -122.4194
        );

        var createResponse = await _proximityClient.CreateBusinessAsync(request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var deleteResponse = await _proximityClient.DeleteBusinessAsync(businessId);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _proximityClient.GetBusinessAsync(businessId);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
