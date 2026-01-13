using System.Diagnostics;

[CollectionDefinition("ProximityTests")]
public class ProximityTestCollection : ICollectionFixture<ProximityTestFixture> {

}

public class ProximityTestFixture : IAsyncLifetime {
    private const string ProximityBaseUrl = "http://localhost:1000";
    public ProximityClient ProximityClient { get; set; } = null!;

    public async Task DisposeAsync() {
        //var process = Process.Start("docker", "compose down");
        //await process.WaitForExitAsync();
        await Task.Yield();
    }

    public async Task InitializeAsync() {
        var process = Process.Start("docker", "compose up -d --build");
        await process.WaitForExitAsync();

        this.ProximityClient = new ProximityClient(ProximityBaseUrl);
        var healthy = await ProximityClient.GetHealthAsync();
        Assert.Equal("healthy", healthy);
    }
}