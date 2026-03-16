using MongoDB.Driver;
using Shared.Mongo.Repositories;

namespace GoogleMaps.Api.Geocoding.Mongo;

public sealed class GeoLocationRepository : IGeoLocationRepository {
    private readonly IMongoRepository<GeoLocationDocument, string> _repository;

    public GeoLocationRepository(IMongoRepository<GeoLocationDocument, string> repository) {
        _repository = repository;
    }

    public Task InsertAsync(GeoLocationDocument document, CancellationToken cancellationToken = default) {
        return _repository.Collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task<GeoLocationDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default) {
        return await _repository.Collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> DeleteByIdAsync(string id, CancellationToken cancellationToken = default) {
        var result = await _repository.Collection.DeleteOneAsync(x => x.Id == id, cancellationToken);
        return result.DeletedCount > 0;
    }
}
