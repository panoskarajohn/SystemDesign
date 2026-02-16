using System.Diagnostics;

namespace NearbyFriends.IntegrationTest;

[CollectionDefinition("NearbyTests")]
public class NearbyTestCollection : ICollectionFixture<NearbyTestFixture> {
}

public class NearbyTestFixture : IAsyncLifetime {
    private const string NearbyBaseUrl = "http://localhost:1001";
    public NearbyClient NearbyClient { get; set; } = null!;

    public async Task DisposeAsync() {
        // var process = Process.Start("docker", "compose down");
        // await process.WaitForExitAsync();
        await Task.Yield();
    }

    public async Task InitializeAsync() {
        var downProcess = Process.Start("docker", "compose down -v");
        await downProcess.WaitForExitAsync();

        var process = Process.Start("docker", "compose up -d --build");
        await process.WaitForExitAsync();

        int counter = 0;
        int max = 100;
        while (counter++ < max) {
            try {
                NearbyClient = new NearbyClient(NearbyBaseUrl);
                var healthy = await NearbyClient.GetHealthAsync();
                Assert.Equal("healthy", healthy);
                return;
            }
            catch {
                await Task.Delay(100);
            }
        }

        throw new TimeoutException("Nearby API did not become healthy in time.");
    }
}
