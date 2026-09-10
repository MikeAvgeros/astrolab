using System.Collections.Immutable;
using AstroLab.Core.Result;
using AstroLab.Core.Sources;

namespace AstroLab.Core.Astrometry;

/// <summary>
/// Computes the similarity transform (offset, rotation, scale) needed to register a target image
/// onto a reference image's pixel grid. Prefers each image's own WCS solution when both are
/// available; otherwise falls back to a translation-only estimate from the images' brightest
/// detected sources, paired by descending flux rank.
/// </summary>
public static class ImageAligner
{
    private const double DegreesToRadians = Math.PI / 180.0;
    private const double DefaultRotationDegrees = 0.0;
    private const double DefaultScale = 1.0;
    private const double FullCircleDegrees = 360.0;
    private const double HalfCircleDegrees = 180.0;

    public static Result<AlignmentTransform> AlignByWcs(Wcs target, Wcs reference)
    {
        var targetScale = (target.PixelScaleXDegrees + target.PixelScaleYDegrees) / 2.0;

        var referenceScale = (reference.PixelScaleXDegrees + reference.PixelScaleYDegrees) / 2.0;

        if (targetScale <= 0.0 || !double.IsFinite(targetScale) || referenceScale <= 0.0 || !double.IsFinite(referenceScale))
        {
            return Error.Validation(
                "images.align.invalid_pixel_scale", "Both images must have a positive, finite WCS pixel scale to compute an alignment transform.");
        }

        var scale = targetScale / referenceScale;

        var rotationDegrees = NormalizeRotationDegrees(reference.RotationDegrees - target.RotationDegrees);

        var mappedResult = reference.WorldToPixel(target.ReferenceRightAscension, target.ReferenceDeclination);

        if (mappedResult.IsFailure)
        {
            return Result<AlignmentTransform>.Failure(mappedResult.Error);
        }

        var (mappedPixelX, mappedPixelY) = mappedResult.Value;

        var rotationRadians = rotationDegrees * DegreesToRadians;

        var cosRotation = Math.Cos(rotationRadians);

        var sinRotation = Math.Sin(rotationRadians);

        var rotatedTargetRefX = cosRotation * target.ReferencePixelX - sinRotation * target.ReferencePixelY;

        var rotatedTargetRefY = sinRotation * target.ReferencePixelX + cosRotation * target.ReferencePixelY;

        var offsetX = mappedPixelX - scale * rotatedTargetRefX;

        var offsetY = mappedPixelY - scale * rotatedTargetRefY;

        return AlignmentTransform.Create(offsetX, offsetY, rotationDegrees, scale);
    }

    public static Result<AlignmentTransform> AlignBySourceCentroids(
        ImmutableArray<DetectedSource> targetSources, ImmutableArray<DetectedSource> referenceSources)
    {
        if (targetSources.IsEmpty || referenceSources.IsEmpty)
        {
            return Error.Validation(
                "images.align.no_reference_points",
                "At least one detected source is required in each image (or a WCS solution on both) to compute an alignment transform.");
        }

        var pairCount = Math.Min(targetSources.Length, referenceSources.Length);

        var offsetXSum = 0.0;

        var offsetYSum = 0.0;

        for (var i = 0; i < pairCount; i++)
        {
            offsetXSum += referenceSources[i].PixelX - targetSources[i].PixelX;

            offsetYSum += referenceSources[i].PixelY - targetSources[i].PixelY;
        }

        return AlignmentTransform.Create(offsetXSum / pairCount, offsetYSum / pairCount, DefaultRotationDegrees, DefaultScale);
    }

    private static double NormalizeRotationDegrees(double degrees)
    {
        var normalized = degrees % FullCircleDegrees;

        if (normalized > HalfCircleDegrees)
        {
            normalized -= FullCircleDegrees;
        }
        else if (normalized <= -HalfCircleDegrees)
        {
            normalized += FullCircleDegrees;
        }

        return normalized;
    }
}
