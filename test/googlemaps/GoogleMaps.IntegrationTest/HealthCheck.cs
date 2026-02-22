namespace GoogleMaps.IntegrationTest;

[Collection("GoogleMapsTests")]
public class HealthCheckTests {
    private readonly GoogleMapsClient _googleMapsClient;

    public HealthCheckTests(GoogleMapsTestFixture fixture) {
        _googleMapsClient = fixture.GoogleMapsClient;
    }

    [Fact]
    public async Task HealthcheckShouldReturnHealthy() {
        var result = await _googleMapsClient.GetHealthAsync();
        Assert.Equal("healthy", result);
    }
}
