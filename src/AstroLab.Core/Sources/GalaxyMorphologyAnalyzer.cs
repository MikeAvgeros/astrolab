using System.Collections.Immutable;
using AstroLab.Core.Photometry;
using AstroLab.Core.Result;

namespace AstroLab.Core.Sources;

/// <summary>
/// Estimates the size, shape, and coarse morphological type of the detected source nearest a
/// requested pixel position. Effective radius and ellipticity come from the same flux-weighted
/// second-moment shape analysis as <see cref="SourceShapeAnalyzer"/>, circularized as
/// sqrt(semiMajorAxis * semiMinorAxis). The morphological type is a concentration-index
/// classification (C = R80/R20, the ratio of the radii enclosing 80% and 20% of the source's
/// curve-of-growth flux) — a standard, lightweight substitute for a full non-linear Sersic profile
/// fit used for automated galaxy classification (e.g. Strateva et al. 2001; Conselice 2003), since a
/// de Vaucouleurs (n=4, elliptical) profile is far more centrally concentrated than an exponential
/// (n=1, disk) profile carrying the same total flux. When the concentration index cannot be measured
/// (the source sits too close to the image edge, or encloses no net positive flux), the
/// morphological type falls back to "Irregular" rather than failing the whole estimate, since the
/// effective radius and ellipticity remain meaningful on their own.
/// </summary>
public static class GalaxyMorphologyAnalyzer
{
    private const double EllipticalConcentrationThreshold = 2.6;
    private const double AnalysisRadiusMultiple = 3.0;
    private const double BackgroundAnnulusMultiple = 1.5;
    private const double MinimumEffectiveRadiusPixels = 1.0;
    private const int CurveOfGrowthSampleCount = 12;
    private const double MinimumSampleFraction = 0.15;
    private const double LowerConcentrationFraction = 0.20;
    private const double UpperConcentrationFraction = 0.80;
    private const string IrregularMorphologicalType = "Irregular";
    private const string EllipticalMorphologicalType = "Elliptical";
    private const string SpiralMorphologicalType = "Spiral";
    private const double Epsilon = 1e-12;

    public static Result<GalaxyMorphologyEstimate> Analyze(ReadOnlySpan<float> pixels, int width, int height, double centerX, double centerY)
    {
        var regionSetResult = SourceDetector.DetectRegions(pixels, width, height);

        if (regionSetResult.IsFailure)
        {
            return Result<GalaxyMorphologyEstimate>.Failure(regionSetResult.Error);
        }

        var candidates = regionSetResult.Value.Candidates;

        if (candidates.IsEmpty)
        {
            return Error.NotFound("sources.galaxymorphology.no_source_found", "No source was detected in this image.");
        }

        var candidate = candidates[FindNearestCandidateIndex(candidates, centerX, centerY)];

        var (semiMajorAxisPixels, semiMinorAxisPixels, ellipticity, _) = SourceShapeAnalyzer.ComputeShape(candidate);

        var effectiveRadiusPixels = Math.Max(MinimumEffectiveRadiusPixels, Math.Sqrt(semiMajorAxisPixels * semiMinorAxisPixels));

        var centroidX = candidate.WeightedXSum / candidate.WeightSum;

        var centroidY = candidate.WeightedYSum / candidate.WeightSum;

        var concentrationResult = MeasureConcentrationIndex(pixels, width, height, centroidX, centroidY, effectiveRadiusPixels);

        var morphologicalType = concentrationResult.Match(
            concentration => concentration >= EllipticalConcentrationThreshold ? EllipticalMorphologicalType : SpiralMorphologicalType,
            _ => IrregularMorphologicalType);

        return GalaxyMorphologyEstimate.Create(effectiveRadiusPixels, ellipticity, morphologicalType);
    }

    private static int FindNearestCandidateIndex(ImmutableArray<SourceCandidate> candidates, double centerX, double centerY)
    {
        var nearestIndex = 0;

        var nearestDistanceSquared = double.PositiveInfinity;

        for (var i = 0; i < candidates.Length; i++)
        {
            var dx = candidates[i].WeightedXSum / candidates[i].WeightSum - centerX;

            var dy = candidates[i].WeightedYSum / candidates[i].WeightSum - centerY;

            var distanceSquared = dx * dx + dy * dy;

            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;

                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private static Result<double> MeasureConcentrationIndex(
        ReadOnlySpan<float> pixels, int width, int height, double centerX, double centerY, double effectiveRadiusPixels)
    {
        var maxRadius = Math.Min(
            AnalysisRadiusMultiple * effectiveRadiusPixels, MaxRadiusWithinBounds(width, height, centerX, centerY));

        if (maxRadius <= 0.0)
        {
            return Error.Validation(
                "sources.galaxymorphology.insufficient_area", "The source is too close to the image edge to measure a concentration index.");
        }

        var annulusResult = ApertureEngine.MeasureAnnulusBackground(
            pixels, width, height, centerX, centerY, maxRadius, maxRadius * BackgroundAnnulusMultiple);

        if (annulusResult.IsFailure)
        {
            return Result<double>.Failure(annulusResult.Error);
        }

        var backgroundPerPixel = annulusResult.Value.BackgroundPerPixel;

        Span<double> radii = stackalloc double[CurveOfGrowthSampleCount];

        Span<double> netFlux = stackalloc double[CurveOfGrowthSampleCount];

        for (var i = 0; i < CurveOfGrowthSampleCount; i++)
        {
            var fraction = MinimumSampleFraction + (1.0 - MinimumSampleFraction) * i / (CurveOfGrowthSampleCount - 1);

            var radius = fraction * maxRadius;

            var apertureResult = ApertureEngine.MeasureCircularAperture(pixels, width, height, centerX, centerY, radius);

            if (apertureResult.IsFailure)
            {
                return Result<double>.Failure(apertureResult.Error);
            }

            radii[i] = radius;

            netFlux[i] = apertureResult.Value.Flux - backgroundPerPixel * apertureResult.Value.Area;
        }

        var totalFlux = netFlux[^1];

        if (totalFlux <= 0.0)
        {
            return Error.Validation(
                "sources.galaxymorphology.non_positive_flux", "No positive net flux was enclosed within the analysis aperture.");
        }

        var r20 = InterpolateRadiusAtFraction(radii, netFlux, totalFlux, LowerConcentrationFraction);

        var r80 = InterpolateRadiusAtFraction(radii, netFlux, totalFlux, UpperConcentrationFraction);

        if (r20 <= 0.0)
        {
            return Error.Validation(
                "sources.galaxymorphology.compact_source", "The source's flux is too centrally compact to resolve a concentration index.");
        }

        return r80 / r20;
    }

    private static double MaxRadiusWithinBounds(int width, int height, double centerX, double centerY) =>
        Math.Min(Math.Min(centerX, width - centerX), Math.Min(centerY, height - centerY));

    private static double InterpolateRadiusAtFraction(
        ReadOnlySpan<double> radii, ReadOnlySpan<double> netFlux, double totalFlux, double targetFraction)
    {
        var targetFlux = targetFraction * totalFlux;

        var previousRadius = 0.0;

        var previousFlux = 0.0;

        for (var i = 0; i < radii.Length; i++)
        {
            var flux = Math.Max(previousFlux, netFlux[i]);

            if (flux >= targetFlux)
            {
                return Math.Abs(flux - previousFlux) < Epsilon
                    ? previousRadius
                    : previousRadius + (targetFlux - previousFlux) / (flux - previousFlux) * (radii[i] - previousRadius);
            }

            previousRadius = radii[i];

            previousFlux = flux;
        }

        return radii[^1];
    }
}
