using System.Net.Http.Json;
using System.Text.Json;

namespace NearbyFriends.IntegrationTest;

public sealed class NearbyClient : IDisposable {
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public NearbyClient(string baseUrl) {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _jsonOptions = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<string> GetHealthAsync() {
        var response = await _httpClient.GetAsync("/api/health");
        return await response.Content.ReadAsStringAsync();
    }

    public Task<HttpResponseMessage> CreateUserAsync(CreateUserRequest request) {
        return _httpClient.PostAsJsonAsync("/api/users", request, _jsonOptions);
    }

    public Task<HttpResponseMessage> GetUserAsync(Guid userId) {
        return _httpClient.GetAsync($"/api/users/{userId}");
    }

    public Task<HttpResponseMessage> UpdateUserAsync(Guid userId, UpdateUserRequest request) {
        return _httpClient.PutAsJsonAsync($"/api/users/{userId}", request, _jsonOptions);
    }

    public Task<HttpResponseMessage> AddFriendAsync(Guid userId, Guid friendId) {
        return _httpClient.PostAsync($"/api/users/{userId}/friends/{friendId}", content: null);
    }

    public async Task<UserResponse?> ReadUserAsync(HttpResponseMessage response) {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) {
            return null;
        }

        return JsonSerializer.Deserialize<UserResponse>(content, _jsonOptions);
    }

    public void Dispose() {
        _httpClient.Dispose();
    }
}
