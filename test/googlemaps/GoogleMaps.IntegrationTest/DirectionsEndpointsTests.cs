using System.Net;

namespace GoogleMaps.IntegrationTest;

[Collection("GoogleMapsTests")]
public class DirectionsEndpointsTests {
    private readonly GoogleMapsClient _googleMapsClient;

    public DirectionsEndpointsTests(GoogleMapsTestFixture fixture) {
        _googleMapsClient = fixture.GoogleMapsClient;
    }

    [Fact]
    public async Task GenerateDirectionsShouldReturnTurnByTurnFromStJohnsWoodStationToStElizabethsHospital() {
        var userId = $"user-{Guid.NewGuid():N}";
        var geolocationIds = new List<string>();

        const string stJohnsWoodInput = "St John's Wood Station, London";
        const string destinationInput = "St Elizabeth's Hospital, London";

        const double originLatitude = 51.53408;
        const double originLongitude = -0.17485;

        try {
            var places = new[] {
                new { Input = stJohnsWoodInput, Latitude = 51.53408, Longitude = -0.17485 },
                new { Input = "Wellington Road, London", Latitude = 51.53395, Longitude = -0.17710 },
                new { Input = "Abercorn Place, London", Latitude = 51.53380, Longitude = -0.17980 },
                new { Input = "St John's Wood Road, London", Latitude = 51.53362, Longitude = -0.18260 },
                new { Input = "Lisson Grove, London", Latitude = 51.53330, Longitude = -0.18515 },
                new { Input = destinationInput, Latitude = 51.53295, Longitude = -0.18785 }
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

            var locationTimestamp = new DateTimeOffset(2026, 2, 26, 10, 0, 0, TimeSpan.Zero);
            var postLocationResponse = await _googleMapsClient.PostLocationsAsync(new PostLocationsRequest(
                userId,
                new[] {
                    new LocationPointRequest(originLatitude, originLongitude, locationTimestamp)
                }
            ));
            Assert.Equal(HttpStatusCode.OK, postLocationResponse.StatusCode);

            var generateResponse = await _googleMapsClient.GenerateDirectionsAsync(new GenerateDirectionsRequest(
                userId,
                destinationInput
            ));

            Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);

            var directions = await _googleMapsClient.ReadGenerateDirectionsResponseAsync(generateResponse);
            Assert.NotNull(directions);
            Assert.Equal(userId, directions!.UserId);
            Assert.Equal(destinationInput, directions.DestinationInput);
            Assert.NotEmpty(directions.Steps);
            Assert.True(directions.TotalDistanceMeters > 0);
            Assert.False(string.IsNullOrWhiteSpace(directions.OriginPlusCode));
            Assert.False(string.IsNullOrWhiteSpace(directions.DestinationPlusCode));
            Assert.All(directions.Steps[..^1], step => Assert.StartsWith("Go ", step.Instruction));
            Assert.Contains(directions.Steps, step => step.Heading.Contains("west", StringComparison.OrdinalIgnoreCase));
            Assert.StartsWith("You reached ", directions.Steps[^1].Instruction);
            Assert.Equal(destinationInput, directions.Steps[^1].TargetInput);
            Assert.Equal(directions.DestinationPlusCode, directions.Steps[^1].ToPlusCode);
            Assert.Equal(0, directions.Steps[^1].DistanceMeters);
        }
        finally {
            await _googleMapsClient.ClearUserLocationsAsync(userId);

            foreach (var geolocationId in geolocationIds) {
                await _googleMapsClient.DeleteGeolocationAsync(geolocationId);
            }
        }
    }
}
