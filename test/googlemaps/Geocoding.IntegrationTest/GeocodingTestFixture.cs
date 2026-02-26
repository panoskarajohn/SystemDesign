using System.Diagnostics;

namespace Geocoding.IntegrationTest;

[CollectionDefinition("GeocodingTests")]
public class GeocodingTestCollection : ICollectionFixture<GeocodingTestFixture> {
}

public class GeocodingTestFixture : IAsyncLifetime {
    private const string GeocodingBaseUrl = "http://localhost:1002";
    public GeocodingClient GeocodingClient { get; set; } = null!;

    public async Task DisposeAsync() {
        await Task.Yield();
    }

    public async Task InitializeAsync() {
        var downProcess = Process.Start("docker", "compose down -v");
        await downProcess.WaitForExitAsync();

        var process = Process.Start("docker", "compose up -d --build");
        await process.WaitForExitAsync();

        var counter = 0;
        const int max = 400;
        while (counter++ < max) {
            try {
                GeocodingClient = new GeocodingClient(GeocodingBaseUrl);
                var healthy = await GeocodingClient.GetHealthAsync();
                Assert.Equal("healthy", healthy);
                return;
            }
            catch {
                await Task.Delay(250);
            }
        }

        throw new TimeoutException("GoogleMaps API (geocoding endpoints) did not become healthy in time.");
    }
}
