using System.Collections.Immutable;
using AstroLab.Core.Result;
using AstroLab.Core.Sources;

namespace AstroLab.Core.Astrometry;

/// <summary>
/// Computes the similarity transform (offset, rotation, scale) needed to register a target image
/// onto a reference image's pixel grid. Prefers each image's own WCS solution when both are
/// available; otherwise falls back to a translation-only estimate from the images' brightest
/// detected sources: every target/reference pairing proposes an offset, the offset under which the
/// most target sources land within a small tolerance of some reference source wins, and the result
/// is the median offset of those matched pairs. Unlike pairing sources by flux rank, this tolerates
/// sources entering or leaving the field and differing detection lists.
/// </summary>
public static class ImageAligner
{
    private const double DegreesToRadians = Math.PI / 180.0;
    private const double DefaultRotationDegrees = 0.0;
    private const double DefaultScale = 1.0;
    private const double FullCircleDegrees = 360.0;
    private const double HalfCircleDegrees = 180.0;
    private const int MaxVotingSources = 30;
    private const int MinimumConsistentMatches = 2;
    private const double MatchTolerancePixels = 2.0;

    public static Result<AlignmentTransform> AlignByWcs(Wcs target, Wcs reference)
    {
        var targetScale = (target.PixelScaleXDegrees + target.PixelScaleYDegrees) / 2.0;

        var referenceScale = (reference.PixelScaleXDegrees + reference.PixelScaleYDegrees) / 2.0;

        if (targetScale <= 0.0 || !double.IsFinite(targetScale) || referenceScale <= 0.0 || !double.IsFinite(referenceScale))
        {
            return Error.Validation(
                "images.align.invalid_pixel_scale", "Both images must have a positive, finite WCS pixel scale to compute an alignment transform.");
        }

        if (target.IsMirrored != reference.IsMirrored)
        {
            return Error.Validation(
                "images.align.mismatched_parity",
                "The target and reference WCS solutions have opposite parity (one is mirrored relative to the other); " +
                "a similarity transform of rotation, uniform scale, and translation cannot register them without an additional reflection.");
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

        var targets = targetSources.AsSpan()[..Math.Min(targetSources.Length, MaxVotingSources)];

        var references = referenceSources.AsSpan()[..Math.Min(referenceSources.Length, MaxVotingSources)];

        var bestOffset = (X: 0.0, Y: 0.0);

        var bestVotes = 0;

        foreach (var target in targets)
        {
            foreach (var reference in references)
            {
                var candidate = (X: reference.PixelX - target.PixelX, Y: reference.PixelY - target.PixelY);

                var votes = CountMatches(targets, references, candidate);

                if (votes > bestVotes)
                {
                    bestVotes = votes;

                    bestOffset = candidate;
                }
            }
        }

        var requiredVotes = Math.Min(MinimumConsistentMatches, Math.Min(targets.Length, references.Length));

        if (bestVotes < requiredVotes)
        {
            return Error.Validation(
                "images.align.no_consistent_offset",
                "No translation brings at least two detected sources of the two images into agreement; the images may not overlap.");
        }

        var (offsetX, offsetY) = RefineOffset(targets, references, bestOffset);

        return AlignmentTransform.Create(offsetX, offsetY, DefaultRotationDegrees, DefaultScale);
    }

    private static int CountMatches(ReadOnlySpan<DetectedSource> targets, ReadOnlySpan<DetectedSource> references, (double X, double Y) offset)
    {
        var matches = 0;

        foreach (var target in targets)
        {
            if (FindMatch(references, target.PixelX + offset.X, target.PixelY + offset.Y) is not null)
            {
                matches++;
            }
        }

        return matches;
    }

    private static (double X, double Y) RefineOffset(
        ReadOnlySpan<DetectedSource> targets, ReadOnlySpan<DetectedSource> references, (double X, double Y) offset)
    {
        var offsetsX = new List<double>(targets.Length);

        var offsetsY = new List<double>(targets.Length);

        foreach (var target in targets)
        {
            if (FindMatch(references, target.PixelX + offset.X, target.PixelY + offset.Y) is { } reference)
            {
                offsetsX.Add(reference.PixelX - target.PixelX);

                offsetsY.Add(reference.PixelY - target.PixelY);
            }
        }

        return (Median(offsetsX), Median(offsetsY));
    }

    private static DetectedSource? FindMatch(ReadOnlySpan<DetectedSource> references, double predictedX, double predictedY)
    {
        DetectedSource? match = null;

        var bestDistanceSquared = MatchTolerancePixels * MatchTolerancePixels;

        foreach (var reference in references)
        {
            var dx = reference.PixelX - predictedX;

            var dy = reference.PixelY - predictedY;

            var distanceSquared = dx * dx + dy * dy;

            if (distanceSquared <= bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;

                match = reference;
            }
        }

        return match;
    }

    private static double Median(List<double> values)
    {
        values.Sort();

        var mid = values.Count / 2;

        return values.Count % 2 == 1 ? values[mid] : (values[mid - 1] + values[mid]) / 2.0;
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
