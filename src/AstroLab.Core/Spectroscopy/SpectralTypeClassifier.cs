using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Pure spectral-type estimation from a 1D spectrum's absorption/emission-line density (detected
/// features per dispersion bin, via <see cref="SpectralLineDetector"/>). Line density is a coarse
/// but physically motivated proxy for spectral complexity: it rises roughly monotonically along the
/// classical Harvard/MK sequence, from hot O-type stars (a handful of strong, broad H/He lines on a
/// blue continuum) to cool M-type stars (dense forests of metal and molecular absorption features).
/// This is a simplified, unweighted line-density classifier, not a template-matching or
/// line-identification pipeline, so its output is always a coarse estimate rather than a precise
/// spectral subtype.
/// </summary>
public static class SpectralTypeClassifier
{
    private static readonly (string SpectralType, double MaximumLineDensity)[] Bands =
    [
        ("O", 0.005),
        ("B", 0.010),
        ("A", 0.018),
        ("F", 0.028),
        ("G", 0.040),
        ("K", 0.055),
        ("M", double.PositiveInfinity),
    ];

    public static Result<(string SpectralType, double Confidence)> Classify(
        ReadOnlySpan<double> spectrum, double significanceSigma = SpectralLineDetector.DefaultSignificanceSigma)
    {
        var spectrumLength = spectrum.Length;

        var detectResult = SpectralLineDetector.Detect(spectrum, significanceSigma);

        return detectResult.Map(lines => ClassifyByLineDensity(lines.Length / (double)spectrumLength));
    }

    private static (string SpectralType, double Confidence) ClassifyByLineDensity(double lineDensity)
    {
        var lowerBound = 0.0;

        foreach (var (spectralType, upperBound) in Bands)
        {
            if (lineDensity < upperBound)
            {
                return (spectralType, ComputeConfidence(lineDensity, lowerBound, upperBound));
            }

            lowerBound = upperBound;
        }

        return (Bands[^1].SpectralType, 1.0);
    }

    private static double ComputeConfidence(double lineDensity, double lowerBound, double upperBound)
    {
        if (double.IsPositiveInfinity(upperBound))
        {
            return 1.0;
        }

        var halfBandWidth = (upperBound - lowerBound) / 2.0;

        var distanceToNearestBoundary = Math.Min(lineDensity - lowerBound, upperBound - lineDensity);

        return Math.Clamp(distanceToNearestBoundary / halfBandWidth, 0.0, 1.0);
    }
}
