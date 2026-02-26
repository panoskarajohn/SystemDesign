namespace GoogleMaps.Api.Geocoding.Contracts;

public record GeoPoint {
    public GeoPoint(double longitude, double latitude) {
        if (double.IsNaN(latitude)
            || double.IsInfinity(latitude)) {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be a finite number.");
        }

        if (double.IsNaN(longitude)
            || double.IsInfinity(longitude)) {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be a finite number.");
        }

        if (latitude < -90.0 || latitude > 90.0) {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude Range(-90, 90)");
        }

        if (longitude < -180 || longitude > 180) {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude Range(-180, 180)");
        }

        Longitude = longitude;
        Latitude = latitude;
    }

    public double Longitude { get; init; }
    public double Latitude { get; init; }

    internal void Deconstruct(out double latitude, out double longitude) {
        latitude = Latitude;
        longitude = Longitude;
    }
}

public class InsertGeolocationRequest {
    public required GeoPoint Location { get; set; }
    public required string Input { get; set; }
    public required string Language { get; set; }
    public required string RegionBias { get; set; }
    public required string Source { get; set; }
}
