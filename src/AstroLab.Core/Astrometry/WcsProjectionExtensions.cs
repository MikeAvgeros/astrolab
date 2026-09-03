namespace AstroLab.Core.Astrometry;

/// <summary>
/// Per-projection math for <see cref="WcsProjection"/>: the FITS <c>CTYPE</c> projection code,
/// and the conversions between a native latitude and a projection-plane radius that every
/// zenithal projection needs but computes differently.
/// </summary>
public static class WcsProjectionExtensions
{
    private const double DegreesPerRadian = 180.0 / Math.PI;
    private const double RightAngleRadians = Math.PI / 2.0;

    extension(WcsProjection projection)
    {
        public string Code => projection switch
        {
            WcsProjection.Tan => "TAN",
            WcsProjection.Sin => "SIN",
            WcsProjection.Arc => "ARC",
            _ => throw new ArgumentOutOfRangeException(nameof(projection)),
        };

        public double NativeLatitudeToRadiusDegrees(double nativeLatitudeRadians) => projection switch
        {
            WcsProjection.Tan => DegreesPerRadian / Math.Tan(nativeLatitudeRadians),
            WcsProjection.Sin => DegreesPerRadian * Math.Cos(nativeLatitudeRadians),
            WcsProjection.Arc => DegreesPerRadian * (RightAngleRadians - nativeLatitudeRadians),
            _ => throw new ArgumentOutOfRangeException(nameof(projection)),
        };

        public double RadiusDegreesToNativeLatitude(double radiusDegrees) => projection switch
        {
            WcsProjection.Tan => Math.Atan2(DegreesPerRadian, radiusDegrees),
            WcsProjection.Sin => Math.Acos(radiusDegrees / DegreesPerRadian),
            WcsProjection.Arc => RightAngleRadians - (radiusDegrees / DegreesPerRadian),
            _ => throw new ArgumentOutOfRangeException(nameof(projection)),
        };
    }

    public static WcsProjection? FromCode(string code) => code switch
    {
        "TAN" => WcsProjection.Tan,
        "SIN" => WcsProjection.Sin,
        "ARC" => WcsProjection.Arc,
        _ => null,
    };
}
