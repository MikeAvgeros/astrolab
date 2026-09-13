using System.Collections.Immutable;
using AstroLab.Core.Result;

namespace AstroLab.Core.Astrometry;

/// <summary>
/// Pure sanity-checking of an already-parsed <see cref="Wcs"/> solution against the image it
/// describes: whether its linear transform is invertible, how far its axes deviate from
/// orthogonality, how asymmetric its per-axis pixel scale is, and how well a pixel position
/// survives a pixel-to-world-to-pixel round trip at the image center.
/// </summary>
public static class WcsValidator
{
    private const double RadiansToDegrees = 180.0 / Math.PI;
    private const double RightAngleDegrees = 90.0;
    private const double HalfTurnDegrees = 180.0;

    private const double OrthogonalityToleranceDegrees = 1.0;
    private const double PixelScaleRatioTolerance = 1.05;
    private const double RoundTripTolerancePixels = 0.01;

    public static Result<WcsValidationReport> Validate(Wcs wcs, int imageWidth, int imageHeight)
    {
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return Error.Validation(
                "astrometry.wcs_validation.invalid_image_dimensions", "imageWidth and imageHeight must both be positive.");
        }

        var determinant = wcs.Determinant;

        var isInvertible = determinant != 0.0;

        var skewDegrees = ComputeSkewDegrees(wcs);

        var pixelScaleRatio = ComputePixelScaleRatio(wcs);

        var roundTripErrorPixels = ComputeRoundTripErrorPixels(wcs, imageWidth, imageHeight);

        var issues = BuildIssues(isInvertible, skewDegrees, pixelScaleRatio, roundTripErrorPixels);

        return WcsValidationReport.Create(isInvertible, determinant, skewDegrees, pixelScaleRatio, roundTripErrorPixels, issues);
    }

    private static double ComputeSkewDegrees(Wcs wcs)
    {
        var columnOneAngle = Math.Atan2(wcs.Cd21, wcs.Cd11);

        var columnTwoAngle = Math.Atan2(wcs.Cd22, wcs.Cd12);

        var angleBetweenDegrees = Math.Abs(columnTwoAngle - columnOneAngle) * RadiansToDegrees % HalfTurnDegrees;

        return Math.Abs(angleBetweenDegrees - RightAngleDegrees);
    }

    private static double ComputePixelScaleRatio(Wcs wcs)
    {
        var scaleX = wcs.PixelScaleXDegrees;

        var scaleY = wcs.PixelScaleYDegrees;

        if (scaleX <= 0.0 || scaleY <= 0.0)
        {
            return double.NaN;
        }

        return Math.Max(scaleX, scaleY) / Math.Min(scaleX, scaleY);
    }

    private static double ComputeRoundTripErrorPixels(Wcs wcs, int imageWidth, int imageHeight)
    {
        var samplePixelX = imageWidth / 2.0;

        var samplePixelY = imageHeight / 2.0;

        var worldResult = wcs.PixelToWorld(samplePixelX, samplePixelY);

        if (worldResult.IsFailure)
        {
            return double.NaN;
        }

        var pixelResult = wcs.WorldToPixel(worldResult.Value.RightAscension, worldResult.Value.Declination);

        if (pixelResult.IsFailure)
        {
            return double.NaN;
        }

        var deltaX = pixelResult.Value.PixelX - samplePixelX;

        var deltaY = pixelResult.Value.PixelY - samplePixelY;

        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    private static ImmutableArray<string> BuildIssues(bool isInvertible, double skewDegrees, double pixelScaleRatio, double roundTripErrorPixels)
    {
        var issues = ImmutableArray.CreateBuilder<string>();

        if (!isInvertible)
        {
            issues.Add("astrometry.wcs_validation.singular_transform");
        }

        if (skewDegrees > OrthogonalityToleranceDegrees)
        {
            issues.Add("astrometry.wcs_validation.non_orthogonal_axes");
        }

        if (double.IsNaN(pixelScaleRatio))
        {
            issues.Add("astrometry.wcs_validation.pixel_scale_not_measurable");
        }
        else if (pixelScaleRatio > PixelScaleRatioTolerance)
        {
            issues.Add("astrometry.wcs_validation.asymmetric_pixel_scale");
        }

        if (double.IsNaN(roundTripErrorPixels))
        {
            issues.Add("astrometry.wcs_validation.round_trip_not_computable");
        }
        else if (roundTripErrorPixels > RoundTripTolerancePixels)
        {
            issues.Add("astrometry.wcs_validation.round_trip_mismatch");
        }

        return issues.ToImmutable();
    }
}
