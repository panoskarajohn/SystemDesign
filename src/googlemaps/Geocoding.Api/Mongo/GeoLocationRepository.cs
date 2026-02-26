using Shared.Mongo.Repositories;

namespace Geocoding.Api.Mongo;

public sealed class GeoLocationRepository : IGeoLocationRepository {
    private readonly IMongoRepository<GeoLocationDocument, string> _repository;

    public GeoLocationRepository(IMongoRepository<GeoLocationDocument, string> repository) {
        _repository = repository;
    }

    public Task InsertAsync(GeoLocationDocument document, CancellationToken cancellationToken = default) {
        return _repository.Collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }
}
