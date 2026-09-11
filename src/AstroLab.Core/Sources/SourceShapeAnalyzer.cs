using System.Collections.Immutable;
using AstroLab.Core.Result;

namespace AstroLab.Core.Sources;

/// <summary>
/// Measures the morphology of every source <see cref="SourceDetector"/> would detect, from the
/// flux-weighted second moments of each region's pixels: semi-major/semi-minor axis lengths,
/// ellipticity, and position angle — the same second-moment shape formulation used by standard
/// source-extraction catalogues (e.g. SExtractor's A_IMAGE/B_IMAGE/THETA_IMAGE/ELLIPTICITY).
/// </summary>
public static class SourceShapeAnalyzer
{
    private const double DegreesPerRadian = 180.0 / Math.PI;

    public static Result<ImmutableArray<SourceShape>> Analyze(
        ReadOnlySpan<float> pixels,
        int width,
        int height,
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources)
    {
        var regionSetResult = SourceDetector.DetectRegions(pixels, width, height, thresholdSigma, minimumArea, maxSources);

        if (regionSetResult.IsFailure)
        {
            return Result<ImmutableArray<SourceShape>>.Failure(regionSetResult.Error);
        }

        var candidates = regionSetResult.Value.Candidates;

        var builder = ImmutableArray.CreateBuilder<SourceShape>(candidates.Length);

        for (var i = 0; i < candidates.Length; i++)
        {
            var (semiMajorAxisPixels, semiMinorAxisPixels, ellipticity, positionAngleDegrees) = ComputeShape(candidates[i]);

            builder.Add(SourceShape.Create(i + 1, semiMajorAxisPixels, semiMinorAxisPixels, ellipticity, positionAngleDegrees));
        }

        return builder.MoveToImmutable();
    }

    internal static (double SemiMajorAxisPixels, double SemiMinorAxisPixels, double Ellipticity, double PositionAngleDegrees) ComputeShape(SourceCandidate candidate)
    {
        var meanX = candidate.WeightedXSum / candidate.WeightSum;

        var meanY = candidate.WeightedYSum / candidate.WeightSum;

        var ixx = Math.Max(0.0, candidate.WeightedXxSum / candidate.WeightSum - meanX * meanX);

        var iyy = Math.Max(0.0, candidate.WeightedYySum / candidate.WeightSum - meanY * meanY);

        var ixy = (candidate.WeightedXySum / candidate.WeightSum) - meanX * meanY;

        var meanMoment = (ixx + iyy) / 2.0;

        var halfDifference = (ixx - iyy) / 2.0;

        var discriminant = Math.Sqrt(halfDifference * halfDifference + ixy * ixy);

        var semiMajorAxisPixels = Math.Sqrt(Math.Max(0.0, meanMoment + discriminant));

        var semiMinorAxisPixels = Math.Sqrt(Math.Max(0.0, meanMoment - discriminant));

        var ellipticity = semiMajorAxisPixels > 0.0 ? 1.0 - semiMinorAxisPixels / semiMajorAxisPixels : 0.0;

        var positionAngleDegrees = 0.5 * Math.Atan2(2.0 * ixy, ixx - iyy) * DegreesPerRadian;

        return (semiMajorAxisPixels, semiMinorAxisPixels, ellipticity, positionAngleDegrees);
    }
}
