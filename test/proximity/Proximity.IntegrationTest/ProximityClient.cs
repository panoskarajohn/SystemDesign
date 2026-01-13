
using System.Net.Http.Json;
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

    public Task<HttpResponseMessage> CreateBusinessAsync(CreateBusinessRequest request) {
        return _httpClient.PostAsJsonAsync("/api/businesses", request, _jsonOptions);
    }

    public Task<HttpResponseMessage> GetBusinessAsync(string businessId) {
        return _httpClient.GetAsync($"/api/businesses/{businessId}");
    }

    public Task<HttpResponseMessage> UpdateBusinessAsync(string businessId, UpdateBusinessRequest request) {
        return _httpClient.PutAsJsonAsync($"/api/businesses/{businessId}", request, _jsonOptions);
    }

    public Task<HttpResponseMessage> DeleteBusinessAsync(string businessId) {
        return _httpClient.DeleteAsync($"/api/businesses/{businessId}");
    }

    public async Task<BusinessResponse?> ReadBusinessAsync(HttpResponseMessage response) {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) {
            return null;
        }

        return JsonSerializer.Deserialize<BusinessResponse>(content, _jsonOptions);
    }

    public void Dispose() {
        _httpClient.Dispose();
    }

}
