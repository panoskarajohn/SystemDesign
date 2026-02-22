namespace GoogleMaps.IntegrationTest;

public sealed class GoogleMapsClient : IDisposable {
    private readonly HttpClient _httpClient;

    public GoogleMapsClient(string baseUrl) {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<string> GetHealthAsync() {
        var response = await _httpClient.GetAsync("/api/health");
        return await response.Content.ReadAsStringAsync();
    }

    public void Dispose() {
        _httpClient.Dispose();
    }
}
