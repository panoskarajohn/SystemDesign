using Microsoft.AspNetCore.Mvc.Testing;

namespace Geocoding.IntegrationTest;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>> {
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckTests(WebApplicationFactory<Program> factory) {
        _factory = factory;
    }

    [Fact]
    public async Task HealthcheckShouldReturnHealthy() {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("healthy", body);
    }
}
