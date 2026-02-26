using System.Net.Http.Json;
using System.Text.Json;

namespace Geocoding.IntegrationTest;

public sealed class GeocodingClient : IDisposable {
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public GeocodingClient(string baseUrl) {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _jsonOptions = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<string> GetHealthAsync() {
        var response = await _httpClient.GetAsync("/api/health");
        return await response.Content.ReadAsStringAsync();
    }

    public Task<HttpResponseMessage> InsertGeolocationAsync(InsertGeolocationRequest request) {
        return _httpClient.PostAsJsonAsync("/v1/geolocations", request, _jsonOptions);
    }

    public Task<HttpResponseMessage> GetGeolocationAsync(string id) {
        return _httpClient.GetAsync($"/v1/geolocations/{Uri.EscapeDataString(id)}");
    }

    public Task<HttpResponseMessage> DeleteGeolocationAsync(string id) {
        return _httpClient.DeleteAsync($"/v1/geolocations/{Uri.EscapeDataString(id)}");
    }

    public async Task<InsertGeolocationResponse?> ReadInsertGeolocationResponseAsync(HttpResponseMessage response) {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) {
            return null;
        }

        return JsonSerializer.Deserialize<InsertGeolocationResponse>(content, _jsonOptions);
    }

    public async Task<GeolocationResponse?> ReadGeolocationResponseAsync(HttpResponseMessage response) {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) {
            return null;
        }

        return JsonSerializer.Deserialize<GeolocationResponse>(content, _jsonOptions);
    }

    public void Dispose() {
        _httpClient.Dispose();
    }
}
