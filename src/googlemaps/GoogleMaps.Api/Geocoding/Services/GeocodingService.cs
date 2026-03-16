using GoogleMaps.Api.Geocoding.Contracts;
using GoogleMaps.Api.Geocoding.Mongo;
using MongoDB.Driver.GeoJsonObjectModel;

namespace GoogleMaps.Api.Geocoding.Services;

public class GeocodingService {
    private readonly IGeoLocationRepository _geoLocationRepository;

    public GeocodingService(IGeoLocationRepository geoLocationRepository) {
        _geoLocationRepository = geoLocationRepository;
    }

    public Task<GeoLocationDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default) {
        return _geoLocationRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<bool> DeleteByIdAsync(string id, CancellationToken cancellationToken = default) {
        return _geoLocationRepository.DeleteByIdAsync(id, cancellationToken);
    }

    public async Task<string> InsertGeocodingResultAsync(InsertGeolocationRequest request, CancellationToken cancellationToken = default) {
        var document = new GeoLocationDocument {
            Input = request.Input.Trim(),
            Language = request.Language.Trim(),
            RegionBias = request.RegionBias.Trim(),
            Source = request.Source.Trim(),
            PlusCode = PlusCode.Encode(request.Location),
            Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                new GeoJson2DGeographicCoordinates(request.Location.Longitude, request.Location.Latitude)
            ),
            Timestamp = DateTimeOffset.UtcNow
        };

        await _geoLocationRepository.InsertAsync(document, cancellationToken);
        return document.Id;
    }
}
