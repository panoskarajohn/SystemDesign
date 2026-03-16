namespace GoogleMaps.IntegrationTest;

public static class TestPlusCode {
    private const string Alphabet = "23456789CFGHJMPQRVWX";
    private const char Separator = '+';
    private const int DigitsCount = 10;
    private const double Ninety = 90.0;
    private const double OneEighty = 180.0;

    private static readonly double[] PairResolutions = { 20.0, 1.0, 0.05, 0.0025, 0.000125 };

    public static GeolocationPointRequest Decode(string plusCode) {
        if (string.IsNullOrWhiteSpace(plusCode)) {
            throw new ArgumentException("Plus code is required.", nameof(plusCode));
        }

        var normalized = plusCode.Trim().ToUpperInvariant().Replace(Separator.ToString(), string.Empty);
        if (normalized.Length != DigitsCount) {
            throw new ArgumentException("Plus code must contain exactly 10 digits (excluding '+').", nameof(plusCode));
        }

        var latitude = -Ninety;
        var longitude = -OneEighty;

        for (var i = 0; i < PairResolutions.Length; i++) {
            var resolution = PairResolutions[i];
            var latDigit = Alphabet.IndexOf(normalized[i * 2]);
            var lonDigit = Alphabet.IndexOf(normalized[(i * 2) + 1]);

            if (latDigit < 0 || lonDigit < 0) {
                throw new ArgumentException("Plus code contains invalid characters.", nameof(plusCode));
            }

            latitude += latDigit * resolution;
            longitude += lonDigit * resolution;
        }

        var cellCenterOffset = PairResolutions[^1] / 2.0;
        return new GeolocationPointRequest(longitude + cellCenterOffset, latitude + cellCenterOffset);
    }
}
