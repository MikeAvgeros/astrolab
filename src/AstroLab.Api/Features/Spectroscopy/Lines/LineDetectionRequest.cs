using AstroLab.Core.Spectroscopy;

namespace AstroLab.Api.Features.Spectroscopy.Lines;

public sealed record LineDetectionRequest
{
    private LineDetectionRequest(double? significanceThreshold, double[]? dispersionCoefficients, int continuumWindowBins)
    {
        SignificanceThreshold = significanceThreshold;
        DispersionCoefficients = dispersionCoefficients;
        ContinuumWindowBins = continuumWindowBins;
    }

    public double? SignificanceThreshold { get; }
    
    public double[]? DispersionCoefficients { get; }

    public int ContinuumWindowBins { get; }

    public static LineDetectionRequest Create(
        double? significanceThreshold = null,
        double[]? dispersionCoefficients = null,
        int continuumWindowBins = SpectralLineDetector.DefaultContinuumWindowBins)
    {
        if (significanceThreshold is { } threshold)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threshold);
        }

        if (dispersionCoefficients is not null && !Array.TrueForAll(dispersionCoefficients, double.IsFinite))
        {
            throw new ArgumentException("dispersionCoefficients must all be finite.", nameof(dispersionCoefficients));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(continuumWindowBins, SpectralLineDetector.MinimumContinuumWindowBins);

        return new LineDetectionRequest(significanceThreshold, dispersionCoefficients, continuumWindowBins);
    }
}
