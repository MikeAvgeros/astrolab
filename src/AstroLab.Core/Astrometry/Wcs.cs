using AstroLab.Core.Fits;
using AstroLab.Core.Result;

namespace AstroLab.Core.Astrometry;

/// <summary>
/// A parsed, validated FITS World Coordinate System solution for a 2D image HDU (Calabretta &amp;
/// Greisen 2002, Papers I &amp; II), supporting conversion between pixel and celestial (RA/Dec)
/// coordinates for the <see cref="WcsProjection.Tan"/>, <see cref="WcsProjection.Sin"/>, and
/// <see cref="WcsProjection.Arc"/> zenithal projections. Immutable and pure — carries no I/O and
/// is not specific to any telescope, instrument, or axis ordering.
/// </summary>
public readonly record struct Wcs
{
    private const double DegreesToRadians = Math.PI / 180.0;
    private const double RadiansToDegrees = 180.0 / Math.PI;
    private const double FullCircleDegrees = 360.0;
    private const double MinDeclinationDegrees = -90.0;
    private const double MaxDeclinationDegrees = 90.0;

    /// <summary>
    /// This API represents pixel positions 0-indexed with pixel centers at half-integer offsets
    /// (pixel <c>0</c> spans <c>[0, 1)</c>, center <c>0.5</c>) — matching
    /// <c>AstroLab.Core.Photometry.ApertureEngine</c> — whereas FITS <c>CRPIXn</c> is 1-indexed
    /// with pixel centers at integer values. <c>FitsPixel = ApiPixel + PixelCenterOffset</c>.
    /// </summary>
    private const double PixelCenterOffset = 0.5;

    private const double DefaultPcDiagonal = 1.0;
    private const double DefaultPcOffDiagonal = 0.0;
    private const double DefaultRotationDegrees = 0.0;
    private const double DefaultCdComponent = 0.0;

    private Wcs(
        string cType1, string cType2, WcsProjection projection,
        double crPix1, double crPix2, double crVal1, double crVal2,
        double cd11, double cd12, double cd21, double cd22,
        int longitudeAxisIndex, int latitudeAxisIndex, string? radeSys)
    {
        CType1 = cType1;
        CType2 = cType2;
        Projection = projection;
        CrPix1 = crPix1;
        CrPix2 = crPix2;
        CrVal1 = crVal1;
        CrVal2 = crVal2;
        Cd11 = cd11;
        Cd12 = cd12;
        Cd21 = cd21;
        Cd22 = cd22;
        LongitudeAxisIndex = longitudeAxisIndex;
        LatitudeAxisIndex = latitudeAxisIndex;
        RadeSys = radeSys;
    }

    public string CType1 { get; }

    public string CType2 { get; }

    public WcsProjection Projection { get; }

    public double CrPix1 { get; }

    public double CrPix2 { get; }

    public double CrVal1 { get; }

    public double CrVal2 { get; }

    public double Cd11 { get; }

    public double Cd12 { get; }

    public double Cd21 { get; }

    public double Cd22 { get; }

    /// <summary>0 when axis 1 is the longitude (RA-like) axis, 1 when axis 2 is.</summary>
    public int LongitudeAxisIndex { get; }

    /// <summary>0 when axis 1 is the latitude (Dec-like) axis, 1 when axis 2 is.</summary>
    public int LatitudeAxisIndex { get; }

    /// <summary>The <c>RADESYS</c> keyword value, or <see langword="null"/> when absent — never guessed.</summary>
    public string? RadeSys { get; }

    /// <summary>Reference right ascension, in degrees (the <c>CRVAL</c> on the longitude axis).</summary>
    public double ReferenceRightAscension => LongitudeAxisIndex == 0 ? CrVal1 : CrVal2;

    /// <summary>Reference declination, in degrees (the <c>CRVAL</c> on the latitude axis).</summary>
    public double ReferenceDeclination => LatitudeAxisIndex == 0 ? CrVal1 : CrVal2;

    /// <summary>Reference pixel X, in this API's 0-indexed pixel-center coordinate convention (see <see cref="PixelToWorld"/>).</summary>
    public double ReferencePixelX => CrPix1 - PixelCenterOffset;

    /// <summary>Reference pixel Y, in this API's 0-indexed pixel-center coordinate convention.</summary>
    public double ReferencePixelY => CrPix2 - PixelCenterOffset;

    /// <summary>Pixel scale along axis 1, in degrees/pixel.</summary>
    public double PixelScaleXDegrees => Math.Sqrt((Cd11 * Cd11) + (Cd21 * Cd21));

    /// <summary>Pixel scale along axis 2, in degrees/pixel.</summary>
    public double PixelScaleYDegrees => Math.Sqrt((Cd12 * Cd12) + (Cd22 * Cd22));

    /// <summary>The rotation of axis 1 relative to celestial north, in degrees, derived from the linear transform matrix.</summary>
    public double RotationDegrees => Math.Atan2(Cd21, Cd11) * RadiansToDegrees;

    /// <summary>
    /// Converts a pixel position — in this API's 0-indexed pixel-center convention, where pixel
    /// <c>(0, 0)</c>'s center is at <c>(0.5, 0.5)</c> — to celestial coordinates.
    /// </summary>
    public Result<(double RightAscension, double Declination)> PixelToWorld(double pixelX, double pixelY)
    {
        var p1 = pixelX + PixelCenterOffset - CrPix1;

        var p2 = pixelY + PixelCenterOffset - CrPix2;

        var iwc1 = (Cd11 * p1) + (Cd12 * p2);

        var iwc2 = (Cd21 * p1) + (Cd22 * p2);

        var xDegrees = LongitudeAxisIndex == 0 ? iwc1 : iwc2;

        var yDegrees = LatitudeAxisIndex == 0 ? iwc1 : iwc2;

        var radiusDegrees = Math.Sqrt((xDegrees * xDegrees) + (yDegrees * yDegrees));

        var radiusCheck = ValidateProjectionRadius(radiusDegrees);

        if (radiusCheck.IsFailure)
        {
            return Result<(double, double)>.Failure(radiusCheck.Error);
        }

        var phi = Math.Atan2(xDegrees, -yDegrees);

        var theta = Projection.RadiusDegreesToNativeLatitude(radiusDegrees);

        var alpha0 = ReferenceRightAscension * DegreesToRadians;

        var delta0 = ReferenceDeclination * DegreesToRadians;

        var sinTheta = Math.Sin(theta);

        var cosTheta = Math.Cos(theta);

        var sinDelta0 = Math.Sin(delta0);

        var cosDelta0 = Math.Cos(delta0);

        var sinPhi = Math.Sin(phi);

        var cosPhi = Math.Cos(phi);

        var declination = Math.Asin(Math.Clamp((sinTheta * sinDelta0) - (cosTheta * cosDelta0 * cosPhi), -1.0, 1.0));

        var rightAscension = alpha0 + Math.Atan2(cosTheta * sinPhi, (sinTheta * cosDelta0) + (cosTheta * sinDelta0 * cosPhi));

        return (NormalizeDegrees(rightAscension * RadiansToDegrees), declination * RadiansToDegrees);
    }

    /// <summary>
    /// Converts celestial coordinates to a pixel position, in this API's 0-indexed pixel-center
    /// convention (see <see cref="PixelToWorld"/>).
    /// </summary>
    public Result<(double PixelX, double PixelY)> WorldToPixel(double rightAscension, double declination)
    {
        if (declination is < MinDeclinationDegrees or > MaxDeclinationDegrees)
        {
            return Error.Validation("astrometry.invalid_declination", "declination must be between -90 and 90 degrees.");
        }

        var alpha = rightAscension * DegreesToRadians;

        var delta = declination * DegreesToRadians;

        var alpha0 = ReferenceRightAscension * DegreesToRadians;

        var delta0 = ReferenceDeclination * DegreesToRadians;

        var deltaAlpha = alpha - alpha0;

        var sinDelta = Math.Sin(delta);

        var cosDelta = Math.Cos(delta);

        var sinDelta0 = Math.Sin(delta0);

        var cosDelta0 = Math.Cos(delta0);

        var sinDeltaAlpha = Math.Sin(deltaAlpha);

        var cosDeltaAlpha = Math.Cos(deltaAlpha);

        var theta = Math.Asin(Math.Clamp((sinDelta * sinDelta0) + (cosDelta * cosDelta0 * cosDeltaAlpha), -1.0, 1.0));

        var phi = Math.PI + Math.Atan2(
            -cosDelta * sinDeltaAlpha,
            (sinDelta * cosDelta0) - (cosDelta * sinDelta0 * cosDeltaAlpha));

        var latitudeCheck = ValidateNativeLatitude(theta);

        if (latitudeCheck.IsFailure)
        {
            return Result<(double, double)>.Failure(latitudeCheck.Error);
        }

        var radiusDegrees = Projection.NativeLatitudeToRadiusDegrees(theta);

        var xDegrees = radiusDegrees * Math.Sin(phi);

        var yDegrees = -radiusDegrees * Math.Cos(phi);

        var iwc1 = LongitudeAxisIndex == 0 ? xDegrees : yDegrees;

        var iwc2 = LatitudeAxisIndex == 0 ? xDegrees : yDegrees;

        var determinant = (Cd11 * Cd22) - (Cd12 * Cd21);

        if (determinant == 0.0)
        {
            return Error.Validation("astrometry.singular_transform", "The WCS linear transform matrix is singular and cannot be inverted.");
        }

        var p1 = ((Cd22 * iwc1) - (Cd12 * iwc2)) / determinant;

        var p2 = ((Cd11 * iwc2) - (Cd21 * iwc1)) / determinant;

        return (CrPix1 + p1 - PixelCenterOffset, CrPix2 + p2 - PixelCenterOffset);
    }

    private Result<Unit> ValidateProjectionRadius(double radiusDegrees) => Projection switch
    {
        WcsProjection.Sin when radiusDegrees > RadiansToDegrees => Error.Validation(
            "astrometry.point_outside_projection", "Pixel position lies outside the valid radius of a SIN projection."),
        _ => Result<Unit>.Success(Unit.Value),
    };

    private Result<Unit> ValidateNativeLatitude(double thetaRadians) => Projection switch
    {
        WcsProjection.Tan when thetaRadians <= 0.0 => PointNotVisibleError("TAN"),
        WcsProjection.Sin when thetaRadians < 0.0 => PointNotVisibleError("SIN"),
        _ => Result<Unit>.Success(Unit.Value),
    };

    private static Error PointNotVisibleError(string projectionCode) => Error.Validation(
        "astrometry.point_not_visible",
        $"The requested sky position is more than 90 degrees from the reference point and is not representable in a {projectionCode} projection.");

    private static double NormalizeDegrees(double degrees)
    {
        var normalized = degrees % FullCircleDegrees;

        return normalized < 0.0 ? normalized + FullCircleDegrees : normalized;
    }

    /// <summary>
    /// Parses a usable WCS solution from an HDU's header, where present. Returns
    /// <see cref="ErrorCategory.NotFound"/> when the required keywords are absent (no WCS ever
    /// attempted, never inferred), <see cref="ErrorCategory.Validation"/> when present keywords
    /// are inconsistent/unusable, and <see cref="ErrorCategory.NotImplemented"/> for a
    /// syntactically valid but unsupported projection code.
    /// </summary>
    public static Result<Wcs> FromHeader(FitsHeader header)
    {
        var cType1Result = header.GetString("CTYPE1");

        var cType2Result = header.GetString("CTYPE2");

        if (cType1Result.IsFailure || cType2Result.IsFailure)
        {
            return Error.NotFound("astrometry.wcs_not_present", "CTYPE1/CTYPE2 were not found; this file carries no usable WCS.");
        }

        var (axis1Type, axis1Projection) = ParseCType(cType1Result.Value);

        var (axis2Type, axis2Projection) = ParseCType(cType2Result.Value);

        var axis1Kind = ClassifyAxis(axis1Type);

        var axis2Kind = ClassifyAxis(axis2Type);

        var celestialAxes = ResolveCelestialAxes(axis1Kind, axis2Kind);

        if (celestialAxes is null)
        {
            return Error.Validation(
                "astrometry.unrecognized_axes",
                $"CTYPE1='{cType1Result.Value}' / CTYPE2='{cType2Result.Value}' do not describe a recognized longitude/latitude celestial axis pair.");
        }

        var (longitudeAxisIndex, latitudeAxisIndex) = celestialAxes.Value;

        var projectionCode = longitudeAxisIndex == 0 ? axis1Projection : axis2Projection;

        var otherProjectionCode = longitudeAxisIndex == 0 ? axis2Projection : axis1Projection;

        if (projectionCode is null || !string.Equals(projectionCode, otherProjectionCode, StringComparison.Ordinal))
        {
            return Error.Validation(
                "astrometry.inconsistent_projection",
                $"CTYPE1/CTYPE2 must specify the same projection code (found '{axis1Projection}' / '{axis2Projection}').");
        }

        var projection = WcsProjectionExtensions.FromCode(projectionCode);

        if (projection is null)
        {
            return Error.NotImplemented(
                "astrometry.unsupported_projection", $"WCS projection '{projectionCode}' is not yet supported (supported: TAN, SIN, ARC).");
        }

        var crPix1 = header.GetReal("CRPIX1");

        var crPix2 = header.GetReal("CRPIX2");

        var crVal1 = header.GetReal("CRVAL1");

        var crVal2 = header.GetReal("CRVAL2");

        if (crPix1.IsFailure || crPix2.IsFailure || crVal1.IsFailure || crVal2.IsFailure)
        {
            return Error.NotFound("astrometry.wcs_not_present", "CRPIX1/CRPIX2/CRVAL1/CRVAL2 must all be present for a usable WCS.");
        }

        var linearTransformResult = ReadLinearTransform(header);

        if (linearTransformResult.IsFailure)
        {
            return Result<Wcs>.Failure(linearTransformResult.Error);
        }

        var (cd11, cd12, cd21, cd22) = linearTransformResult.Value;

        var radeSysResult = header.GetString("RADESYS");

        var radeSys = radeSysResult.IsSuccess ? radeSysResult.Value : null;

        return Create(
            cType1Result.Value, cType2Result.Value, projection.Value,
            crPix1.Value, crPix2.Value, crVal1.Value, crVal2.Value,
            cd11, cd12, cd21, cd22, longitudeAxisIndex, latitudeAxisIndex, radeSys);
    }

    private static (string AxisType, string? ProjectionCode) ParseCType(string cType)
    {
        var trimmed = cType.Trim();

        var axisType = (trimmed.Length >= 4 ? trimmed[..4] : trimmed).TrimEnd('-');

        var projectionCode = trimmed.Length >= 8 ? trimmed[5..8] : null;

        return (axisType, projectionCode);
    }

    private static WcsAxisKind ClassifyAxis(string axisType) => axisType switch
    {
        "RA" => WcsAxisKind.Longitude,
        "DEC" => WcsAxisKind.Latitude,
        _ when axisType.EndsWith("LON", StringComparison.Ordinal) => WcsAxisKind.Longitude,
        _ when axisType.EndsWith("LAT", StringComparison.Ordinal) => WcsAxisKind.Latitude,
        _ => WcsAxisKind.Other,
    };

    private static (int LongitudeAxisIndex, int LatitudeAxisIndex)? ResolveCelestialAxes(WcsAxisKind axis1Kind, WcsAxisKind axis2Kind)
    {
        if (axis1Kind == WcsAxisKind.Longitude && axis2Kind == WcsAxisKind.Latitude)
        {
            return (0, 1);
        }

        if (axis1Kind == WcsAxisKind.Latitude && axis2Kind == WcsAxisKind.Longitude)
        {
            return (1, 0);
        }

        return null;
    }

    /// <summary>
    /// Resolves the 2x2 linear pixel-to-intermediate-world-coordinate matrix (degrees/pixel), per
    /// the FITS WCS convention priority: an explicit <c>CD</c> matrix; else <c>CDELT</c> scaled by
    /// a <c>PC</c> matrix (default identity); else the legacy <c>CDELT</c> + <c>CROTA2</c> convention.
    /// </summary>
    private static Result<(double Cd11, double Cd12, double Cd21, double Cd22)> ReadLinearTransform(FitsHeader header)
    {
        var cd11 = header.GetReal("CD1_1");

        var cd12 = header.GetReal("CD1_2");

        var cd21 = header.GetReal("CD2_1");

        var cd22 = header.GetReal("CD2_2");

        if (cd11.IsSuccess || cd12.IsSuccess || cd21.IsSuccess || cd22.IsSuccess)
        {
            return (
                cd11.GetValueOrDefault(DefaultCdComponent), cd12.GetValueOrDefault(DefaultCdComponent),
                cd21.GetValueOrDefault(DefaultCdComponent), cd22.GetValueOrDefault(DefaultCdComponent));
        }

        var cdelt1 = header.GetReal("CDELT1");

        var cdelt2 = header.GetReal("CDELT2");

        if (cdelt1.IsFailure || cdelt2.IsFailure)
        {
            return Error.NotFound(
                "astrometry.wcs_not_present", "No CD matrix, and CDELT1/CDELT2 are not both present; this file carries no usable pixel scale.");
        }

        var pc11 = header.GetReal("PC1_1");

        var pc12 = header.GetReal("PC1_2");

        var pc21 = header.GetReal("PC2_1");

        var pc22 = header.GetReal("PC2_2");

        if (pc11.IsSuccess || pc12.IsSuccess || pc21.IsSuccess || pc22.IsSuccess)
        {
            var p11 = pc11.GetValueOrDefault(DefaultPcDiagonal);

            var p12 = pc12.GetValueOrDefault(DefaultPcOffDiagonal);

            var p21 = pc21.GetValueOrDefault(DefaultPcOffDiagonal);

            var p22 = pc22.GetValueOrDefault(DefaultPcDiagonal);

            return (cdelt1.Value * p11, cdelt1.Value * p12, cdelt2.Value * p21, cdelt2.Value * p22);
        }

        var crota2 = header.GetReal("CROTA2").GetValueOrDefault(DefaultRotationDegrees) * DegreesToRadians;

        var cosRotation = Math.Cos(crota2);

        var sinRotation = Math.Sin(crota2);

        return (
            cdelt1.Value * cosRotation, -cdelt2.Value * sinRotation,
            cdelt1.Value * sinRotation, cdelt2.Value * cosRotation);
    }

    private static Wcs Create(
        string cType1, string cType2, WcsProjection projection,
        double crPix1, double crPix2, double crVal1, double crVal2,
        double cd11, double cd12, double cd21, double cd22,
        int longitudeAxisIndex, int latitudeAxisIndex, string? radeSys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cType1);

        ArgumentException.ThrowIfNullOrWhiteSpace(cType2);

        return new Wcs(
            cType1, cType2, projection, crPix1, crPix2, crVal1, crVal2,
            cd11, cd12, cd21, cd22, longitudeAxisIndex, latitudeAxisIndex, radeSys);
    }
}
