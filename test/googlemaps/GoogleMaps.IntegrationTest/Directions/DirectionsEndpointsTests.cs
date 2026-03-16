using System.Net;

namespace GoogleMaps.IntegrationTest;

[Collection("GoogleMapsTests")]
public class DirectionsEndpointsTests {
    private readonly GoogleMapsClient _googleMapsClient;

    public DirectionsEndpointsTests(GoogleMapsTestFixture fixture) {
        _googleMapsClient = fixture.GoogleMapsClient;
    }

    [Fact]
    public async Task GenerateRouteShouldReturnMinimalDrawInstructionsFromStJohnsWoodStationToStElizabethsHospital() {
        var geolocationIds = new List<string>();

        const string originInput = "St John's Wood Station, London";
        const string destinationInput = "St John & St Elizabeth Hospital, London";

        try {
            var places = new[] {
                new { Input = originInput, Latitude = 51.53408, Longitude = -0.17485 },
                new { Input = destinationInput, Latitude = 51.53467, Longitude = -0.18438 }
            };

            foreach (var place in places) {
                var insertGeoResponse = await _googleMapsClient.InsertGeolocationAsync(new InsertGeolocationRequest(
                    new GeolocationPointRequest(place.Longitude, place.Latitude),
                    place.Input,
                    "en",
                    "gb",
                    "integration-test"
                ));

                Assert.Equal(HttpStatusCode.Accepted, insertGeoResponse.StatusCode);

                var insertGeoPayload = await _googleMapsClient.ReadInsertGeolocationResponseAsync(insertGeoResponse);
                Assert.NotNull(insertGeoPayload);
                Assert.False(string.IsNullOrWhiteSpace(insertGeoPayload!.Id));
                geolocationIds.Add(insertGeoPayload.Id);

                var getGeoResponse = await _googleMapsClient.GetGeolocationAsync(insertGeoPayload.Id);
                Assert.Equal(HttpStatusCode.OK, getGeoResponse.StatusCode);

                var geolocation = await _googleMapsClient.ReadGeolocationResponseAsync(getGeoResponse);
                Assert.NotNull(geolocation);
                Assert.False(string.IsNullOrWhiteSpace(geolocation!.PlusCode));
            }

            var generateResponse = await _googleMapsClient.GenerateRouteAsync(new GenerateRouteRequest(
                originInput,
                destinationInput
            ));

            Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);

            var route = await _googleMapsClient.ReadGenerateRouteResponseAsync(generateResponse);
            Assert.NotNull(route);
            Assert.Equal(originInput, route!.OriginInput);
            Assert.Equal(destinationInput, route.DestinationInput);
            Assert.True(route.TotalDistanceMeters > 0);

            Assert.Equal(2, route.Path.Count);
            Assert.Equal(originInput, route.Path[0].Label);
            Assert.Equal(destinationInput, route.Path[1].Label);
            Assert.False(string.IsNullOrWhiteSpace(route.Path[0].PlusCode));
            Assert.False(string.IsNullOrWhiteSpace(route.Path[1].PlusCode));

            Assert.Single(route.Segments);
            Assert.StartsWith("Go ", route.Segments[0].Instruction);
            Assert.True(route.Segments[0].DistanceMeters > 0);

            Assert.Equal("polyline", route.Drawing.Type);
            Assert.Equal("Draw one polyline using the coordinates in order.", route.Drawing.Instruction);
            Assert.Equal(route.Path.Count, route.Drawing.Coordinates.Count);
            Assert.Equal(route.Path[0].PlusCode, route.Drawing.Coordinates[0].PlusCode);
            Assert.Equal(route.Path[1].PlusCode, route.Drawing.Coordinates[1].PlusCode);
        }
        finally {
            foreach (var geolocationId in geolocationIds) {
                await _googleMapsClient.DeleteGeolocationAsync(geolocationId);
            }
        }
    }
}
