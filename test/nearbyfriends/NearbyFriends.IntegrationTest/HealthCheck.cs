namespace NearbyFriends.IntegrationTest;

[Collection("NearbyTests")]
public class HealthCheckTests {
    private readonly NearbyClient _nearbyClient;

    public HealthCheckTests(NearbyTestFixture fixture) {
        _nearbyClient = fixture.NearbyClient;
    }

    [Fact]
    public async Task HealthcheckShouldReturnHealthy() {
        var result = await _nearbyClient.GetHealthAsync();
        Assert.Equal("healthy", result);
    }
}
