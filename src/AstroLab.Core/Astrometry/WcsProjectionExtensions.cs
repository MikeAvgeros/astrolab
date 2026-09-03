namespace AstroLab.Core.Astrometry;

public static class WcsProjectionExtensions
{
    private const double DegreesPerRadian = 180.0 / Math.PI;
    private const double RightAngleRadians = Math.PI / 2.0;

    extension(WcsProjection projection)
    {
        /// <summary>The 3-letter FITS <c>CTYPE</c> projection code (e.g. <c>"TAN"</c>).</summary>
        public string Code => projection switch
        {
            WcsProjection.Tan => "TAN",
            WcsProjection.Sin => "SIN",
            WcsProjection.Arc => "ARC",
            _ => throw new ArgumentOutOfRangeException(nameof(projection)),
        };

        /// <summary>The projection-plane radius (degrees) corresponding to a native latitude (radians).</summary>
        public double NativeLatitudeToRadiusDegrees(double nativeLatitudeRadians) => projection switch
        {
            WcsProjection.Tan => DegreesPerRadian / Math.Tan(nativeLatitudeRadians),
            WcsProjection.Sin => DegreesPerRadian * Math.Cos(nativeLatitudeRadians),
            WcsProjection.Arc => DegreesPerRadian * (RightAngleRadians - nativeLatitudeRadians),
            _ => throw new ArgumentOutOfRangeException(nameof(projection)),
        };

        /// <summary>
        /// The native latitude (radians) corresponding to a projection-plane radius (degrees).
        /// Callers must independently validate the radius lies within the projection's domain —
        /// see the per-projection domain checks in <see cref="Wcs"/>.
        /// </summary>
        public double RadiusDegreesToNativeLatitude(double radiusDegrees) => projection switch
        {
            WcsProjection.Tan => Math.Atan2(DegreesPerRadian, radiusDegrees),
            WcsProjection.Sin => Math.Acos(radiusDegrees / DegreesPerRadian),
            WcsProjection.Arc => RightAngleRadians - (radiusDegrees / DegreesPerRadian),
            _ => throw new ArgumentOutOfRangeException(nameof(projection)),
        };
    }

    /// <summary>Resolves a 3-letter FITS <c>CTYPE</c> projection code to a supported <see cref="WcsProjection"/>, or <see langword="null"/> when unrecognized/unsupported.</summary>
    public static WcsProjection? FromCode(string code) => code switch
    {
        "TAN" => WcsProjection.Tan,
        "SIN" => WcsProjection.Sin,
        "ARC" => WcsProjection.Arc,
        _ => null,
    };
}
