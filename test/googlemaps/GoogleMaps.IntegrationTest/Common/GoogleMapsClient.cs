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

    public Task<HttpResponseMessage> ClearUserLocationsAsync(string userId) {
        return _httpClient.DeleteAsync($"/v1/locations/{Uri.EscapeDataString(userId)}");
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

    public Task<HttpResponseMessage> GenerateDirectionsAsync(GenerateDirectionsRequest request) {
        return _httpClient.PostAsJsonAsync("/v1/directions", request, _jsonOptions);
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

    public async Task<GenerateDirectionsResponse?> ReadGenerateDirectionsResponseAsync(HttpResponseMessage response) {
        var content = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<GenerateDirectionsResponse>(content, _jsonOptions);
    }

    public void Dispose() {
        _httpClient.Dispose();
    }
}
