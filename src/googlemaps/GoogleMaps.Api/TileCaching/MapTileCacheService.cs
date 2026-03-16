using System.Net.Http.Json;
using System.Text;

namespace GoogleMaps.Api.TileCaching;

public interface IMapTileCacheService {
    Task<byte[]> GetTileAsync(int z, int x, int y, CancellationToken cancellationToken);
    Task CacheTileAsync(int z, int x, int y, byte[] tileData, CancellationToken cancellationToken);
    Task<bool> TileExistsAsync(int z, int x, int y, CancellationToken cancellationToken);
}

public sealed class MapTileCacheService : IMapTileCacheService {
    private readonly ILogger<MapTileCacheService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _minioEndpoint;
    private readonly string _minioBucket;
    private readonly string _minioAccessKey;
    private readonly string _minioSecretKey;
    private readonly string _localFallbackPath;

    public MapTileCacheService(
        ILogger<MapTileCacheService> logger,
        HttpClient httpClient,
        IConfiguration configuration) {
        _logger = logger;
        _httpClient = httpClient;
        
        _minioEndpoint = configuration["MinIO:Endpoint"] ?? "http://swift:9000";
        _minioBucket = configuration["MinIO:Bucket"] ?? "map-tiles";
        _minioAccessKey = configuration["MinIO:AccessKey"] ?? "minioadmin";
        _minioSecretKey = configuration["MinIO:SecretKey"] ?? "minioadmin";
        _localFallbackPath = configuration["TileCache:BasePath"] ?? "/app/tile_cache";
        
        // Ensure local fallback directory exists
        Directory.CreateDirectory(_localFallbackPath);
        
        // Initialize MinIO bucket
        _ = InitializeMinIOBucketAsync();
    }

    public async Task<byte[]> GetTileAsync(int z, int x, int y, CancellationToken cancellationToken) {
        var tileKey = GetTileKey(z, x, y);

        try {
            // Try to get from MinIO first
            var tileData = await GetTileFromMinIOAsync(tileKey, cancellationToken);
            if (tileData != null) {
                _logger.LogInformation($"Retrieved tile {tileKey} from MinIO cache");
                return tileData;
            }

            // Fallback to local cache
            var localPath = Path.Combine(_localFallbackPath, tileKey);
            if (File.Exists(localPath)) {
                _logger.LogInformation($"Retrieved tile {tileKey} from local fallback cache");
                return await File.ReadAllBytesAsync(localPath, cancellationToken);
            }

            _logger.LogInformation($"Tile {tileKey} not in cache, fetching from tile provider");
            var newTileData = await FetchTileFromProviderAsync(z, x, y, cancellationToken);
            await CacheTileAsync(z, x, y, newTileData, cancellationToken);
            return newTileData;
        }
        catch (Exception ex) {
            _logger.LogError(ex, $"Error getting tile {tileKey}");
            throw;
        }
    }

    public async Task CacheTileAsync(int z, int x, int y, byte[] tileData, CancellationToken cancellationToken) {
        var tileKey = GetTileKey(z, x, y);

        try {
            // Cache to MinIO (primary)
            await CacheTileToMinIOAsync(tileKey, tileData, cancellationToken);

            // Also cache locally as fallback
            var localPath = Path.Combine(_localFallbackPath, tileKey);
            var directory = Path.GetDirectoryName(localPath);
            if (directory != null) {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllBytesAsync(localPath, tileData, cancellationToken);
            
            _logger.LogInformation($"Cached tile {tileKey} to MinIO and local storage");
        }
        catch (Exception ex) {
            _logger.LogError(ex, $"Error caching tile {tileKey}");
        }
    }

    public async Task<bool> TileExistsAsync(int z, int x, int y, CancellationToken cancellationToken) {
        var tileKey = GetTileKey(z, x, y);

        try {
            // Check MinIO first
            var existsInMinIO = await TileExistsInMinIOAsync(tileKey, cancellationToken);
            if (existsInMinIO) {
                return true;
            }

            // Check local fallback
            var localPath = Path.Combine(_localFallbackPath, tileKey);
            return File.Exists(localPath);
        }
        catch (Exception ex) {
            _logger.LogError(ex, $"Error checking tile existence {tileKey}");
            return false;
        }
    }

    private async Task<byte[]?> GetTileFromMinIOAsync(string tileKey, CancellationToken cancellationToken) {
        try {
            var url = $"{_minioEndpoint}/{_minioBucket}/{tileKey}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddAuthHeaders(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode) {
                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            }

            return null;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, $"Failed to get tile from MinIO: {tileKey}");
            return null;
        }
    }

    private async Task CacheTileToMinIOAsync(string tileKey, byte[] tileData, CancellationToken cancellationToken) {
        try {
            var url = $"{_minioEndpoint}/{_minioBucket}/{tileKey}";
            var request = new HttpRequestMessage(HttpMethod.Put, url) {
                Content = new ByteArrayContent(tileData)
            };
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            AddAuthHeaders(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode) {
                _logger.LogInformation($"Cached tile {tileKey} to MinIO");
                return;
            }

            _logger.LogWarning($"Failed to cache tile to MinIO: {tileKey} - Status: {response.StatusCode} - Using local fallback");
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, $"Error caching tile to MinIO: {tileKey} - Using local fallback");
        }
    }

    private async Task<bool> TileExistsInMinIOAsync(string tileKey, CancellationToken cancellationToken) {
        try {
            var url = $"{_minioEndpoint}/{_minioBucket}/{tileKey}";
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            AddAuthHeaders(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch {
            return false;
        }
    }

    private void AddAuthHeaders(HttpRequestMessage request) {
        // MinIO S3 Authentication - add basic auth and S3 compatible headers
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_minioAccessKey}:{_minioSecretKey}"));
        request.Headers.Add("Authorization", $"Basic {credentials}");
        request.Headers.Add("X-Amz-Content-Sha256", "UNSIGNED-PAYLOAD");
    }

    private async Task InitializeMinIOBucketAsync() {
        try {
            // Try to create bucket if it doesn't exist
            var url = $"{_minioEndpoint}/{_minioBucket}";
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            AddAuthHeaders(request);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Conflict) {
                _logger.LogInformation($"MinIO bucket '{_minioBucket}' is ready");
            }
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to initialize MinIO bucket, will use local cache fallback");
        }
    }

    private async Task<byte[]> FetchTileFromProviderAsync(int z, int x, int y, CancellationToken cancellationToken) {
        // Try OSM.de first
        try {
            var url = $"https://tile.openstreetmap.de/tiles/osmde/{z}/{x}/{y}.png";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "SystemDesign-MapTileCache/1.0 (+http://localhost:1002)");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode) {
                _logger.LogInformation($"Fetched tile {z}/{x}/{y} from OSM DE");
                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            }
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, $"Failed to fetch from OSM DE, trying CartoDB");
        }

        // Fallback to CartoDB
        return await FetchTileFromCartoDBAsync(z, x, y, cancellationToken);
    }

    private async Task<byte[]> FetchTileFromCartoDBAsync(int z, int x, int y, CancellationToken cancellationToken) {
        var url = $"https://cartodb-basemaps-a.global.ssl.fastly.net/light_all/{z}/{x}/{y}.png";

        try {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "SystemDesign-MapTileCache/1.0 (+http://localhost:1002)");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode) {
                throw new Exception($"Failed to fetch tile from CartoDB: {response.StatusCode}");
            }

            _logger.LogInformation($"Fetched tile {z}/{x}/{y} from CartoDB");
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex) {
            _logger.LogError(ex, $"Error fetching tile {z}/{x}/{y} from CartoDB");
            throw;
        }
    }

    private static string GetTileKey(int z, int x, int y) => $"tiles/{z}/{x}/{y}.png";
}
