using System.Net.Http.Json;
using System.Text.Json;

namespace GoogleMaps.IntegrationTest;

public sealed class GoogleMapsClient : IDisposable {
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public GoogleMapsClient(string baseUrl) {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _jsonOptions = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<string> GetHealthAsync() {
        var response = await _httpClient.GetAsync("/api/health");
        return await response.Content.ReadAsStringAsync();
    }

    public Task<HttpResponseMessage> PostLocationsAsync(PostLocationsRequest request) {
        return _httpClient.PostAsJsonAsync("/v1/locations", request, _jsonOptions);
    }

    public Task<HttpResponseMessage> GetLastLocationAsync(string userId) {
        return _httpClient.GetAsync($"/v1/locations/{Uri.EscapeDataString(userId)}/last");
    }

    public async Task<PostLocationsResponse?> ReadPostLocationsResponseAsync(HttpResponseMessage response) {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) {
            return null;
        }

        return JsonSerializer.Deserialize<PostLocationsResponse>(content, _jsonOptions);
    }

    public async Task<LastUserLocationResponse?> ReadLastLocationResponseAsync(HttpResponseMessage response) {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) {
            return null;
        }

        return JsonSerializer.Deserialize<LastUserLocationResponse>(content, _jsonOptions);
    }

    public void Dispose() {
        _httpClient.Dispose();
    }
}
