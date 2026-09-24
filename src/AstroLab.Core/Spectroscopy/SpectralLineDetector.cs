using System.Collections.Immutable;
using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Pure spectral line detection over a 1D flux spectrum. The continuum is estimated locally as a
/// running median over a window of <c>continuumWindowBins</c> dispersion bins, and the noise as
/// 1.4826 × the median absolute deviation of the continuum-subtracted residuals; contiguous runs of
/// residuals exceeding the significance threshold are reported with their peak deviation and
/// half-maximum width. A local continuum is what makes the detector work on real spectra: a single
/// global median/spread treats a sloped or curved continuum as signal, either hiding real lines
/// behind an inflated noise estimate or flagging the continuum's extremes as lines.
/// <para>
/// The median window must be several times wider than the lines being sought (a line wider than
/// about half the window is absorbed into the continuum). Near the spectrum's ends the window shrinks
/// symmetrically about each bin, so a linear continuum is still followed exactly, at the cost of lines
/// in the outermost bins being indistinguishable from continuum. The reported line "position" is the
/// dispersion-bin index; without a wavelength calibration this is not a physical wavelength, and
/// callers that have a dispersion solution are responsible for converting it (see
/// <see cref="SpectrumExtractor.EvaluateWavelength"/>).
/// </para>
/// </summary>
public static class SpectralLineDetector
{
    public const double DefaultSignificanceSigma = 5.0;
    public const int DefaultContinuumWindowBins = 101;
    public const int MinimumContinuumWindowBins = 3;
    private const double HalfMaximumFraction = 0.5;
    private const double MadToSigmaFactor = 1.4826;

    public static Result<ImmutableArray<DetectedSpectralLine>> Detect(
        ReadOnlySpan<double> spectrum,
        double significanceSigma = DefaultSignificanceSigma,
        int continuumWindowBins = DefaultContinuumWindowBins)
    {
        if (spectrum.IsEmpty)
        {
            return Error.Validation("spectroscopy.lines.empty_spectrum", "The spectrum contains no bins to search for lines.");
        }

        if (significanceSigma <= 0.0 || !double.IsFinite(significanceSigma))
        {
            return Error.Validation("spectroscopy.lines.invalid_threshold", "significanceSigma must be a finite, positive value.");
        }

        if (continuumWindowBins < MinimumContinuumWindowBins)
        {
            return Error.Validation(
                "spectroscopy.lines.invalid_continuum_window",
                $"continuumWindowBins must be at least {MinimumContinuumWindowBins}.");
        }

        foreach (var value in spectrum)
        {
            if (!double.IsFinite(value))
            {
                return Error.Validation("spectroscopy.lines.non_finite_value", "Spectrum values must be finite.");
            }
        }

        var residuals = SubtractRunningMedianContinuum(spectrum, continuumWindowBins / 2);

        var thresholdValue = significanceSigma * EstimateNoiseSigma(residuals);

        var builder = ImmutableArray.CreateBuilder<DetectedSpectralLine>();

        var index = 0;

        while (index < residuals.Length)
        {
            if (Math.Abs(residuals[index]) <= thresholdValue)
            {
                index++;

                continue;
            }

            var runStart = index;

            while (index < residuals.Length && Math.Abs(residuals[index]) > thresholdValue)
            {
                index++;
            }

            var runEnd = index - 1;

            var peakIndex = runStart;

            for (var i = runStart + 1; i <= runEnd; i++)
            {
                if (Math.Abs(residuals[i]) > Math.Abs(residuals[peakIndex]))
                {
                    peakIndex = i;
                }
            }

            var lineFlux = residuals[peakIndex];

            var fwhm = EstimateFwhm(residuals, peakIndex, lineFlux);

            builder.Add(DetectedSpectralLine.Create(peakIndex, lineFlux, fwhm));
        }

        var lines = builder.ToArray();

        Array.Sort(lines, (left, right) => Math.Abs(right.Flux).CompareTo(Math.Abs(left.Flux)));

        return ImmutableArray.Create(lines);
    }

    private static double[] SubtractRunningMedianContinuum(ReadOnlySpan<double> spectrum, int maxHalfWindow)
    {
        var residuals = new double[spectrum.Length];

        var window = new double[2 * maxHalfWindow + 1];

        for (var i = 0; i < spectrum.Length; i++)
        {
            var halfWindow = Math.Min(maxHalfWindow, Math.Min(i, spectrum.Length - 1 - i));

            var activeWindow = window.AsSpan(0, 2 * halfWindow + 1);

            spectrum.Slice(i - halfWindow, activeWindow.Length).CopyTo(activeWindow);

            residuals[i] = spectrum[i] - Median(activeWindow);
        }

        return residuals;
    }

    private static double EstimateNoiseSigma(ReadOnlySpan<double> residuals)
    {
        var absoluteResiduals = new double[residuals.Length];

        for (var i = 0; i < residuals.Length; i++)
        {
            absoluteResiduals[i] = Math.Abs(residuals[i]);
        }

        return MadToSigmaFactor * Median(absoluteResiduals);
    }

    private static double Median(Span<double> values)
    {
        values.Sort();

        var mid = values.Length / 2;

        return values.Length % 2 == 1 ? values[mid] : (values[mid - 1] + values[mid]) / 2.0;
    }

    private static double EstimateFwhm(ReadOnlySpan<double> residuals, int peakIndex, double lineFlux)
    {
        var halfLevel = HalfMaximumFraction * lineFlux;

        var leftEdge = WalkToHalfMaximum(residuals, peakIndex, halfLevel, step: -1);

        var rightEdge = WalkToHalfMaximum(residuals, peakIndex, halfLevel, step: 1);

        return rightEdge - leftEdge;
    }

    private static double WalkToHalfMaximum(ReadOnlySpan<double> residuals, int peakIndex, double halfLevel, int step)
    {
        var isEmissionPeak = residuals[peakIndex] > halfLevel;

        var previousIndex = peakIndex;

        var currentIndex = peakIndex + step;

        while (currentIndex >= 0 && currentIndex < residuals.Length)
        {
            var crossedHalfMaximum = isEmissionPeak
                ? residuals[currentIndex] <= halfLevel
                : residuals[currentIndex] >= halfLevel;

            if (crossedHalfMaximum)
            {
                var previousValue = residuals[previousIndex];

                var currentValue = residuals[currentIndex];

                var fraction = currentValue == previousValue ? 0.0 : (halfLevel - previousValue) / (currentValue - previousValue);

                return previousIndex + (step * fraction);
            }

            previousIndex = currentIndex;

            currentIndex += step;
        }

        return previousIndex;
    }
}
