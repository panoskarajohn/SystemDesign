using System.Net;
using Geocoding.Api.Geocoding;

namespace Geocoding.IntegrationTest;

[Collection("GeocodingTests")]
public class GeolocationEndpointsTests {
    private readonly GeocodingClient _geocodingClient;

    public GeolocationEndpointsTests(GeocodingTestFixture fixture) {
        _geocodingClient = fixture.GeocodingClient;
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

            var insertResponse = await _geocodingClient.InsertGeolocationAsync(insertRequest);
            Assert.Equal(HttpStatusCode.Accepted, insertResponse.StatusCode);

            var insertPayload = await _geocodingClient.ReadInsertGeolocationResponseAsync(insertResponse);
            Assert.NotNull(insertPayload);
            Assert.False(string.IsNullOrWhiteSpace(insertPayload!.Id));
            geolocationId = insertPayload.Id;

            var getResponse = await _geocodingClient.GetGeolocationAsync(geolocationId);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var geolocation = await _geocodingClient.ReadGeolocationResponseAsync(getResponse);
            Assert.NotNull(geolocation);
            Assert.Equal(geolocationId, geolocation!.Id);
            Assert.False(string.IsNullOrWhiteSpace(geolocation.PlusCode));

            var decoded = PlusCode.Decode(geolocation.PlusCode);
            Assert.Equal(latitude, decoded.Latitude, precision: 4);
            Assert.Equal(longitude, decoded.Longitude, precision: 4);
        }
        finally {
            if (!string.IsNullOrWhiteSpace(geolocationId)) {
                await _geocodingClient.DeleteGeolocationAsync(geolocationId);
            }
        }
    }
}
