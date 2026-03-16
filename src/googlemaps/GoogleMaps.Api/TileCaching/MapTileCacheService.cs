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
    private readonly string _cacheBasePath;

    public MapTileCacheService(
        ILogger<MapTileCacheService> logger,
        HttpClient httpClient,
        IConfiguration configuration) {
        _logger = logger;
        _httpClient = httpClient;
        
        // Use a local file-based cache for tiles (mounted as Docker volume)
        _cacheBasePath = configuration["TileCache:BasePath"] ?? "/app/tile_cache";
        
        // Ensure cache directory exists
        Directory.CreateDirectory(_cacheBasePath);
    }

    public async Task<byte[]> GetTileAsync(int z, int x, int y, CancellationToken cancellationToken) {
        var tileKey = GetTileKey(z, x, y);
        var tilePath = Path.Combine(_cacheBasePath, tileKey);

        try {
            // Check if tile is cached locally
            if (File.Exists(tilePath)) {
                _logger.LogInformation($"Retrieved tile {tileKey} from local cache");
                return await File.ReadAllBytesAsync(tilePath, cancellationToken);
            }

            _logger.LogInformation($"Tile {tileKey} not in cache, fetching from OpenStreetMap");
            var tileData = await FetchTileFromOpenStreetMapAsync(z, x, y, cancellationToken);
            await CacheTileAsync(z, x, y, tileData, cancellationToken);
            return tileData;
        }
        catch (Exception ex) {
            _logger.LogError(ex, $"Error getting tile {tileKey}");
            throw;
        }
    }

    public async Task CacheTileAsync(int z, int x, int y, byte[] tileData, CancellationToken cancellationToken) {
        var tileKey = GetTileKey(z, x, y);
        var tilePath = Path.Combine(_cacheBasePath, tileKey);

        try {
            // Create directory structure if needed
            var directory = Path.GetDirectoryName(tilePath);
            if (directory != null) {
                Directory.CreateDirectory(directory);
            }

            // Write tile to disk
            await File.WriteAllBytesAsync(tilePath, tileData, cancellationToken);
            _logger.LogInformation($"Cached tile {tileKey} to local storage");
        }
        catch (Exception ex) {
            _logger.LogError(ex, $"Error caching tile {tileKey}");
        }
    }

    public async Task<bool> TileExistsAsync(int z, int x, int y, CancellationToken cancellationToken) {
        var tileKey = GetTileKey(z, x, y);
        var tilePath = Path.Combine(_cacheBasePath, tileKey);

        try {
            return File.Exists(tilePath);
        }
        catch (Exception ex) {
            _logger.LogError(ex, $"Error checking tile existence {tileKey}");
            return false;
        }
    }

    private async Task<byte[]> FetchTileFromOpenStreetMapAsync(int z, int x, int y, CancellationToken cancellationToken) {
        // Using Stamen Terrain tiles (allows caching) or CartoDB
        // Alternatives: https://tile.openstreetmap.de/tiles/osmde/{z}/{x}/{y}.png (allows caching)
        // Or CartoDB: https://cartodb-basemaps-a.global.ssl.fastly.net/light_all/{z}/{x}/{y}.png
        var url = $"https://tile.openstreetmap.de/tiles/osmde/{z}/{x}/{y}.png";

        try {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Add proper User-Agent as per tile server policies
            request.Headers.Add("User-Agent", "SystemDesign-MapTileCache/1.0 (+http://localhost:1002)");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode) {
                _logger.LogWarning($"Failed to fetch from osmde, trying CartoDB fallback");
                // Fallback to CartoDB
                return await FetchTileFromCdnAsync(z, x, y, cancellationToken);
            }

            var tileData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            _logger.LogInformation($"Fetched tile {z}/{x}/{y} from OSM DE");
            return tileData;
        }
        catch (Exception ex) {
            _logger.LogError(ex, $"Error fetching tile {z}/{x}/{y} from OSM DE, trying fallback");
            return await FetchTileFromCdnAsync(z, x, y, cancellationToken);
        }
    }

    private async Task<byte[]> FetchTileFromCdnAsync(int z, int x, int y, CancellationToken cancellationToken) {
        // Fallback to CartoDB (allows caching for offline use)
        var url = $"https://cartodb-basemaps-a.global.ssl.fastly.net/light_all/{z}/{x}/{y}.png";

        try {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "SystemDesign-MapTileCache/1.0 (+http://localhost:1002)");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode) {
                throw new Exception($"Failed to fetch tile from CartoDB: {response.StatusCode}");
            }

            var tileData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            _logger.LogInformation($"Fetched tile {z}/{x}/{y} from CartoDB");
            return tileData;
        }
        catch (Exception ex) {
            _logger.LogError(ex, $"Error fetching tile {z}/{x}/{y} from CartoDB");
            throw;
        }
    }

    private static string GetTileKey(int z, int x, int y) => $"tiles/{z}/{x}/{y}.png";
}
