namespace Geocoding.Api.Mongo;

public interface IGeoLocationRepository {
    Task InsertAsync(GeoLocationDocument document, CancellationToken cancellationToken = default);
}
