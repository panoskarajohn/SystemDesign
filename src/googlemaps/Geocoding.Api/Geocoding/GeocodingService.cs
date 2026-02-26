
using Geocoding.Api.Contracts;

namespace Geocoding.Api.Geocoding;

public class GeocodingService {

    public GeocodingService() {
    }

    public async Task InsertGeocodingResultAsync(InsertGeolocationRequest request) {
        throw new NotImplementedException();
    }
    private static string encodePosition(double latitude, double longitude) {
        return "";

    }
}

public static class PlusCode {
    // Open Location Code alphabet (base-20).
    // See: https://github.com/google/open-location-code
    private const string Alphabet = "23456789CFGHJMPQRVWX";
    private const double ninety = 90.0;
    private const double oneeighty = 180.0;
    private const double threesixty = 360.0;

    // Resolution (degrees) for each pair in a 10-digit code (5 pairs).
    // Pair 1: 20°, Pair 2: 1°, Pair 3: 0.05°, Pair 4: 0.0025°, Pair 5: 0.000125°
    private static readonly double[] PairResolutions = { 20.0, 1.0, 0.05, 0.0025, 0.000125 };

    /// <summary>
    /// Encode latitude/longitude into a 10-digit global Plus Code (Open Location Code),
    /// with the '+' separator after 8 digits, e.g. "9C3XGRMG+Q9".
    /// </summary>
    public static string Encode(GeoPoint geopoint) {
        (double latitude, double longitude) = geopoint;

        //90.0 is technically valid, but the encoding grid is half-open at the top edge
        if (latitude == ninety) {
            latitude = ninety - 1e-12;
        }

        //normalize longitude
        longitude = ((longitude + oneeighty) % threesixty + threesixty) % threesixty - oneeighty;
        if (longitude == oneeighty) {
            longitude = -oneeighty;
        }

        // Shift to positive ranges for encoding
        double lat = latitude + ninety;
        double lon = longitude + oneeighty;

        Span<char> output = stackalloc char[11];
        int outIdx = 0;
        int digitsWritten = 0;

        for (int i = 0; i < PairResolutions.Length; i++) {
            double res = PairResolutions[i];

            int latDigit = (int)Math.Floor(lat / res);
            int lonDigit = (int)Math.Floor(lon / res);

            lat -= latDigit * res;
            lon -= lonDigit * res;

            output[outIdx++] = Alphabet[latDigit];
            digitsWritten++;

            output[outIdx++] = Alphabet[lonDigit];
            digitsWritten++;

            if (digitsWritten == 0) {
                output[outIdx++] = '+';
            }
        }

        return new string(output);
    }
}
