using System.Net;

namespace GoogleMaps.IntegrationTest;

[Collection("GoogleMapsTests")]
public class LocationEndpointsTests {
    private readonly GoogleMapsClient _googleMapsClient;

    public LocationEndpointsTests(GoogleMapsTestFixture fixture) {
        _googleMapsClient = fixture.GoogleMapsClient;
    }

    [Fact]
    public async Task PostLocationsAndGetLastLocationShouldReturnLatestPoint() {
        var userId = $"user-{Guid.NewGuid():N}";
        var baseTime = new DateTimeOffset(2026, 2, 22, 12, 0, 0, TimeSpan.Zero);
        var locations = Enumerable.Range(0, 15)
            .Select(i => new LocationPointRequest(
                37.7749 + (i * 0.0001),
                -122.4194 - (i * 0.0001),
                baseTime.AddSeconds(i)))
            .ToArray();

        var request = new PostLocationsRequest(
            userId,
            locations
        );

        var postResponse = await _googleMapsClient.PostLocationsAsync(request);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var postPayload = await _googleMapsClient.ReadPostLocationsResponseAsync(postResponse);
        Assert.NotNull(postPayload);
        Assert.Equal(userId, postPayload!.UserId);
        Assert.Equal(15, postPayload.InsertedCount);

        var getResponse = await _googleMapsClient.GetLastLocationAsync(userId);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var lastLocation = await _googleMapsClient.ReadLastLocationResponseAsync(getResponse);
        Assert.NotNull(lastLocation);
        Assert.Equal(userId, lastLocation!.UserId);
        var expectedLast = locations[^1];
        Assert.Equal(expectedLast.Latitude, lastLocation.Latitude, precision: 4);
        Assert.Equal(expectedLast.Longitude, lastLocation.Longitude, precision: 4);
        Assert.Equal(expectedLast.Timestamp, lastLocation.Timestamp);
    }
}
