using System.Net;

namespace GoogleMaps.IntegrationTest;

[Collection("GoogleMapsTests")]
public class GeolocationEndpointsTests {
    private readonly GoogleMapsClient _googleMapsClient;

    public GeolocationEndpointsTests(GoogleMapsTestFixture fixture) {
        _googleMapsClient = fixture.GoogleMapsClient;
    }

    [Fact]
    public async Task InsertAndGetGeolocationShouldPersistPlusCodeThatDecodesToCoordinates() {
        const double latitude = 37.7749;
        const double longitude = -122.4194;

        string? geolocationId = null;
        try {
            var insertRequest = new InsertGeolocationRequest(
                new GeolocationPointRequest(longitude, latitude),
                "san francisco",
                "en",
                "us",
                "integration-test"
            );

            var insertResponse = await _googleMapsClient.InsertGeolocationAsync(insertRequest);
            Assert.Equal(HttpStatusCode.Accepted, insertResponse.StatusCode);

            var insertPayload = await _googleMapsClient.ReadInsertGeolocationResponseAsync(insertResponse);
            Assert.NotNull(insertPayload);
            Assert.False(string.IsNullOrWhiteSpace(insertPayload!.Id));
            geolocationId = insertPayload.Id;

            var getResponse = await _googleMapsClient.GetGeolocationAsync(geolocationId);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var geolocation = await _googleMapsClient.ReadGeolocationResponseAsync(getResponse);
            Assert.NotNull(geolocation);
            Assert.Equal(geolocationId, geolocation!.Id);
            Assert.False(string.IsNullOrWhiteSpace(geolocation.PlusCode));

            var decoded = TestPlusCode.Decode(geolocation.PlusCode);
            Assert.Equal(latitude, decoded.Latitude, precision: 4);
            Assert.Equal(longitude, decoded.Longitude, precision: 4);
        }
        finally {
            if (!string.IsNullOrWhiteSpace(geolocationId)) {
                await _googleMapsClient.DeleteGeolocationAsync(geolocationId);
            }
        }
    }
}
