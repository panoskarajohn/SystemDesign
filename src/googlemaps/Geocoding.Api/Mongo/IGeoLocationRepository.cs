namespace Geocoding.Api.Mongo;

public interface IGeoLocationRepository {
    Task InsertAsync(GeoLocationDocument document, CancellationToken cancellationToken = default);
    Task<GeoLocationDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> DeleteByIdAsync(string id, CancellationToken cancellationToken = default);
}
