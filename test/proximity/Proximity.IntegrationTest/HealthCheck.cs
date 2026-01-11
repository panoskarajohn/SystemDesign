namespace Proximity.IntegrationTest;

[Collection("ProximityTests")]
public class HealthCheckTests {
    private readonly ProximityClient _proximityClient;

    public HealthCheckTests(ProximityTestFixture fixture) {
        _proximityClient = fixture.ProximityClient;
    }

    [Fact]
    public async Task HealthcheckShouldReturnHealthy() {
        var result = await _proximityClient.GetHealthAsync();
        Assert.Equal("healthy", result);
    }
}
