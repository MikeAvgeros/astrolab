using System.Collections.Immutable;
using AstroLab.Core.Photometry;
using AstroLab.Core.Result;

namespace AstroLab.Core.Sources;

/// <summary>
/// Estimates the size, shape, and coarse morphological type of the detected source at a requested
/// pixel position. Ellipticity comes from the same flux-weighted second-moment shape analysis as
/// <see cref="SourceShapeAnalyzer"/>. Size and concentration follow the SDSS Petrosian system
/// (Blanton et al. 2001; Strateva et al. 2001): the Petrosian radius r_P is where the local surface
/// brightness in the annulus [0.8r, 1.25r] falls to 0.2 × the mean surface brightness within r, the
/// Petrosian flux is the background-subtracted flux within 2 r_P, and R50/R90 are the radii
/// enclosing 50% and 90% of that flux. The effective radius reported is the Petrosian half-light
/// radius R50, and the morphological type is a concentration-index classification (C = R90/R50) — a
/// standard, lightweight substitute for a full non-linear Sersic profile fit used for automated
/// galaxy classification, since a de Vaucouleurs (n=4, elliptical) profile is far more centrally
/// concentrated than an exponential (n=1, disk) profile. The classification threshold of 2.6 is the
/// published dividing line for this Petrosian R90/R50 ratio; it does not apply to other concentration
/// definitions (e.g. Conselice 2003's C = 5*log10(R80/R20)). When the Petrosian aperture cannot be
/// measured (the source sits too close to the image edge, or encloses no net positive flux), the
/// effective radius and concentration are reported as unavailable and the morphological type falls
/// back to "Irregular", since the ellipticity remains meaningful on its own.
/// </summary>
public static class GalaxyMorphologyAnalyzer
{
    public const string MethodName = "Petrosian concentration index (R90/R50) with flux-weighted second-moment shape";

    private const double EllipticalConcentrationThreshold = 2.6;
    private const double PetrosianRatioThreshold = 0.2;
    private const double PetrosianInnerAnnulusFactor = 0.8;
    private const double PetrosianOuterAnnulusFactor = 1.25;
    private const double PetrosianApertureMultiple = 2.0;
    private const double MinimumProfileRadiusPixels = 0.5;
    private const int CurveOfGrowthSampleCount = 60;
    private const double MatchRadiusMultiple = 2.0;
    private const double MinimumMatchRadiusPixels = 3.0;
    private const double LowerConcentrationFraction = 0.50;
    private const double UpperConcentrationFraction = 0.90;
    private const string IrregularMorphologicalType = "Irregular";
    private const string EllipticalMorphologicalType = "Elliptical";
    private const string SpiralMorphologicalType = "Spiral";

    public static Result<GalaxyMorphologyEstimate> Analyze(ReadOnlySpan<float> pixels, int width, int height, double centerX, double centerY)
    {
        if (!double.IsFinite(centerX) || !double.IsFinite(centerY))
        {
            return Error.Validation("sources.galaxymorphology.invalid_position", "centerX and centerY must be finite.");
        }

        var regionSetResult = SourceDetector.DetectRegions(pixels, width, height, maxSources: int.MaxValue);

        if (regionSetResult.IsFailure)
        {
            return Result<GalaxyMorphologyEstimate>.Failure(regionSetResult.Error);
        }

        var regionSet = regionSetResult.Value;

        if (regionSet.Candidates.IsEmpty)
        {
            return Error.NotFound("sources.galaxymorphology.no_source_found", "No source was detected in this image.");
        }

        if (FindCandidateAtPosition(regionSet.Candidates, centerX, centerY) is not { } candidateIndex)
        {
            return Error.NotFound(
                "sources.galaxymorphology.no_source_at_position", "No detected source covers the requested pixel position.");
        }

        var candidate = regionSet.Candidates[candidateIndex];

        var (_, _, ellipticity, _) = SourceShapeAnalyzer.ComputeShape(candidate);

        var centroidX = candidate.WeightedXSum / candidate.WeightSum;

        var centroidY = candidate.WeightedYSum / candidate.WeightSum;

        var petrosianResult = MeasurePetrosianRadii(pixels, width, height, centroidX, centroidY, regionSet.Background);

        if (petrosianResult.IsFailure)
        {
            return GalaxyMorphologyEstimate.Create(null, ellipticity, IrregularMorphologicalType, null);
        }

        var (r50, r90) = petrosianResult.Value;

        var concentration = r90 / r50;

        var morphologicalType = concentration >= EllipticalConcentrationThreshold ? EllipticalMorphologicalType : SpiralMorphologicalType;

        return GalaxyMorphologyEstimate.Create(r50, ellipticity, morphologicalType, concentration);
    }

    private static int? FindCandidateAtPosition(ImmutableArray<SourceCandidate> candidates, double centerX, double centerY)
    {
        int? nearestIndex = null;

        var nearestDistanceSquared = double.PositiveInfinity;

        for (var i = 0; i < candidates.Length; i++)
        {
            var dx = candidates[i].WeightedXSum / candidates[i].WeightSum - centerX;

            var dy = candidates[i].WeightedYSum / candidates[i].WeightSum - centerY;

            var distanceSquared = dx * dx + dy * dy;

            var matchRadius = Math.Max(MinimumMatchRadiusPixels, MatchRadiusMultiple * Math.Sqrt(candidates[i].PixelCount / Math.PI));

            if (distanceSquared <= matchRadius * matchRadius && distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;

                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private static Result<(double R50, double R90)> MeasurePetrosianRadii(
        ReadOnlySpan<float> pixels, int width, int height, double centerX, double centerY, double backgroundPerPixel)
    {
        var maxRadius = MaxRadiusWithinBounds(width, height, centerX, centerY);

        if (maxRadius <= MinimumProfileRadiusPixels * PetrosianApertureMultiple)
        {
            return EdgeError();
        }

        Span<double> radii = stackalloc double[CurveOfGrowthSampleCount];

        Span<double> netFlux = stackalloc double[CurveOfGrowthSampleCount];

        var logRange = Math.Log(maxRadius / MinimumProfileRadiusPixels);

        for (var i = 0; i < CurveOfGrowthSampleCount; i++)
        {
            var radius = MinimumProfileRadiusPixels * Math.Exp(logRange * i / (CurveOfGrowthSampleCount - 1));

            var apertureResult = ApertureEngine.MeasureCircularAperture(pixels, width, height, centerX, centerY, radius);

            if (apertureResult.IsFailure)
            {
                return Result<(double, double)>.Failure(apertureResult.Error);
            }

            radii[i] = radius;

            netFlux[i] = apertureResult.Value.Flux - backgroundPerPixel * apertureResult.Value.Area;
        }

        if (FindPetrosianRadius(radii, netFlux) is not { } petrosianRadius)
        {
            return EdgeError();
        }

        var petrosianApertureRadius = PetrosianApertureMultiple * petrosianRadius;

        if (petrosianApertureRadius > maxRadius)
        {
            return EdgeError();
        }

        var petrosianFlux = InterpolateFlux(radii, netFlux, petrosianApertureRadius);

        if (petrosianFlux <= 0.0)
        {
            return Error.Validation(
                "sources.galaxymorphology.non_positive_flux", "No positive net flux was enclosed within the Petrosian aperture.");
        }

        var r50 = InterpolateRadiusAtFlux(radii, netFlux, LowerConcentrationFraction * petrosianFlux);

        var r90 = InterpolateRadiusAtFlux(radii, netFlux, UpperConcentrationFraction * petrosianFlux);

        return (r50, r90);
    }

    private static Error EdgeError() => Error.Validation(
        "sources.galaxymorphology.insufficient_area",
        "The source is too close to the image edge (or too extended) to measure its Petrosian aperture.");

    private static double MaxRadiusWithinBounds(int width, int height, double centerX, double centerY) =>
        Math.Min(Math.Min(centerX, width - centerX), Math.Min(centerY, height - centerY));

    private static double? FindPetrosianRadius(ReadOnlySpan<double> radii, ReadOnlySpan<double> netFlux)
    {
        var maxEvaluableRadius = radii[^1] / PetrosianOuterAnnulusFactor;

        double? previousRadius = null;

        var previousRatio = 0.0;

        for (var i = 0; i < radii.Length && radii[i] <= maxEvaluableRadius; i++)
        {
            var radius = radii[i];

            var enclosedFlux = netFlux[i];

            if (enclosedFlux <= 0.0 || radius * PetrosianInnerAnnulusFactor < radii[0])
            {
                continue;
            }

            var innerRadius = PetrosianInnerAnnulusFactor * radius;

            var outerRadius = PetrosianOuterAnnulusFactor * radius;

            var annulusFlux = InterpolateFlux(radii, netFlux, outerRadius) - InterpolateFlux(radii, netFlux, innerRadius);

            var localSurfaceBrightness = annulusFlux / (Math.PI * (outerRadius * outerRadius - innerRadius * innerRadius));

            var meanSurfaceBrightness = enclosedFlux / (Math.PI * radius * radius);

            var ratio = localSurfaceBrightness / meanSurfaceBrightness;

            if (ratio <= PetrosianRatioThreshold)
            {
                return previousRadius is { } lastRadius
                    ? lastRadius + (previousRatio - PetrosianRatioThreshold) / (previousRatio - ratio) * (radius - lastRadius)
                    : radius;
            }

            previousRadius = radius;

            previousRatio = ratio;
        }

        return null;
    }

    private static double InterpolateFlux(ReadOnlySpan<double> radii, ReadOnlySpan<double> netFlux, double radius)
    {
        if (radius <= radii[0])
        {
            return netFlux[0] * (radius * radius) / (radii[0] * radii[0]);
        }

        for (var i = 1; i < radii.Length; i++)
        {
            if (radius <= radii[i])
            {
                var fraction = (radius - radii[i - 1]) / (radii[i] - radii[i - 1]);

                return netFlux[i - 1] + fraction * (netFlux[i] - netFlux[i - 1]);
            }
        }

        return netFlux[^1];
    }

    private static double InterpolateRadiusAtFlux(ReadOnlySpan<double> radii, ReadOnlySpan<double> netFlux, double targetFlux)
    {
        var previousRadius = 0.0;

        var previousFlux = 0.0;

        for (var i = 0; i < radii.Length; i++)
        {
            var flux = Math.Max(previousFlux, netFlux[i]);

            if (flux >= targetFlux)
            {
                return flux == previousFlux
                    ? previousRadius
                    : previousRadius + (targetFlux - previousFlux) / (flux - previousFlux) * (radii[i] - previousRadius);
            }

            previousRadius = radii[i];

            previousFlux = flux;
        }

        return radii[^1];
    }
}
