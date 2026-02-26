using System.Diagnostics;

namespace GoogleMaps.IntegrationTest;

[CollectionDefinition("GoogleMapsTests")]
public class GoogleMapsTestCollection : ICollectionFixture<GoogleMapsTestFixture> {
}

public class GoogleMapsTestFixture : IAsyncLifetime {
    private const string GoogleMapsBaseUrl = "http://localhost:1002";
    public GoogleMapsClient GoogleMapsClient { get; set; } = null!;

    public async Task DisposeAsync() {
        await Task.Yield();
    }

    public async Task InitializeAsync() {
        var downProcess = Process.Start("docker", "compose down -v");
        await downProcess.WaitForExitAsync();

        var process = Process.Start("docker", "compose up -d --build");
        await process.WaitForExitAsync();

        int counter = 0;
        const int max = 100;
        while (counter++ < max) {
            try {
                GoogleMapsClient = new GoogleMapsClient(GoogleMapsBaseUrl);
                var healthy = await GoogleMapsClient.GetHealthAsync();
                Assert.Equal("healthy", healthy);
                return;
            }
            catch {
                await Task.Delay(100);
            }
        }

        throw new TimeoutException("GoogleMaps API did not become healthy in time.");
    }
}
