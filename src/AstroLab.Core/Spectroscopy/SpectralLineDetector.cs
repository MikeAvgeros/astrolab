using System.Collections.Immutable;
using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Pure spectral line detection over a 1D flux spectrum: estimates a robust continuum (median) and
/// noise level (inter-quartile-range-based sigma), flags contiguous runs of bins deviating from the
/// continuum by more than a significance threshold, and reports each run's peak deviation and
/// half-maximum width. The reported line "position" is the dispersion-bin index; without a
/// wavelength calibration this is not a physical wavelength, and callers that have a dispersion
/// solution are responsible for converting it (see <see cref="SpectrumExtractor.EvaluateWavelength"/>).
/// </summary>
public static class SpectralLineDetector
{
    public const double DefaultSignificanceSigma = 5.0;

    private const double LowerPercentile = 25.0;
    private const double UpperPercentile = 75.0;
    private const double IqrToSigmaFactor = 1.349;
    private const double HalfMaximumFraction = 0.5;

    public static Result<ImmutableArray<DetectedSpectralLine>> Detect(
        ReadOnlySpan<double> spectrum, double significanceSigma = DefaultSignificanceSigma)
    {
        if (spectrum.IsEmpty)
        {
            return Error.Validation("spectroscopy.lines.empty_spectrum", "The spectrum contains no bins to search for lines.");
        }

        if (significanceSigma <= 0.0 || !double.IsFinite(significanceSigma))
        {
            return Error.Validation("spectroscopy.lines.invalid_threshold", "significanceSigma must be a finite, positive value.");
        }

        var (continuum, sigma) = ComputeRobustStatistics(spectrum);

        var thresholdValue = significanceSigma * sigma;

        var builder = ImmutableArray.CreateBuilder<DetectedSpectralLine>();

        var index = 0;

        while (index < spectrum.Length)
        {
            if (Math.Abs(spectrum[index] - continuum) <= thresholdValue)
            {
                index++;

                continue;
            }

            var runStart = index;

            while (index < spectrum.Length && Math.Abs(spectrum[index] - continuum) > thresholdValue)
            {
                index++;
            }

            var runEnd = index - 1;

            var peakIndex = runStart;

            for (var i = runStart + 1; i <= runEnd; i++)
            {
                if (Math.Abs(spectrum[i] - continuum) > Math.Abs(spectrum[peakIndex] - continuum))
                {
                    peakIndex = i;
                }
            }

            var lineFlux = spectrum[peakIndex] - continuum;

            var fwhm = EstimateFwhm(spectrum, peakIndex, continuum, lineFlux);

            builder.Add(DetectedSpectralLine.Create(peakIndex, lineFlux, fwhm));
        }

        var lines = builder.ToArray();

        Array.Sort(lines, (left, right) => Math.Abs(right.Flux).CompareTo(Math.Abs(left.Flux)));

        return ImmutableArray.Create(lines);
    }

    private static (double Continuum, double Sigma) ComputeRobustStatistics(ReadOnlySpan<double> spectrum)
    {
        var sorted = spectrum.ToArray();

        Array.Sort(sorted);

        var median = Percentile(sorted, 50.0);

        var q1 = Percentile(sorted, LowerPercentile);

        var q3 = Percentile(sorted, UpperPercentile);

        return (median, (q3 - q1) / IqrToSigmaFactor);
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        var rank = (percentile / 100.0) * (sorted.Length - 1);

        var lowerIndex = (int)Math.Floor(rank);

        var upperIndex = (int)Math.Ceiling(rank);

        if (lowerIndex == upperIndex)
        {
            return sorted[lowerIndex];
        }

        var fraction = rank - lowerIndex;

        return sorted[lowerIndex] + (fraction * (sorted[upperIndex] - sorted[lowerIndex]));
    }

    private static double EstimateFwhm(ReadOnlySpan<double> spectrum, int peakIndex, double continuum, double lineFlux)
    {
        var halfLevel = continuum + (HalfMaximumFraction * lineFlux);

        var leftEdge = WalkToHalfMaximum(spectrum, peakIndex, halfLevel, step: -1);

        var rightEdge = WalkToHalfMaximum(spectrum, peakIndex, halfLevel, step: 1);

        return rightEdge - leftEdge;
    }

    private static double WalkToHalfMaximum(ReadOnlySpan<double> spectrum, int peakIndex, double halfLevel, int step)
    {
        var isEmissionPeak = spectrum[peakIndex] > halfLevel;

        var previousIndex = peakIndex;

        var currentIndex = peakIndex + step;

        while (currentIndex >= 0 && currentIndex < spectrum.Length)
        {
            var crossedHalfMaximum = isEmissionPeak
                ? spectrum[currentIndex] <= halfLevel
                : spectrum[currentIndex] >= halfLevel;

            if (crossedHalfMaximum)
            {
                var previousValue = spectrum[previousIndex];

                var currentValue = spectrum[currentIndex];

                var fraction = currentValue == previousValue ? 0.0 : (halfLevel - previousValue) / (currentValue - previousValue);

                return previousIndex + (step * fraction);
            }

            previousIndex = currentIndex;

            currentIndex += step;
        }

        return previousIndex;
    }
}
