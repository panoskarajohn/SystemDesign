
using System.Text.Json;

public sealed class ProximityClient : IDisposable {
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProximityClient(string baseUrl) {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _jsonOptions = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<string> GetHealthAsync() {
        var response = await _httpClient.GetAsync("/api/health");

        var str = await response.Content.ReadAsStringAsync();
        return str;
    }

    public void Dispose() {
        _httpClient.Dispose();
    }

}