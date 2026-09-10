namespace AstroLab.Api.Features.Spectroscopy.Lines;

public sealed record LineDetectionRequest
{
    private LineDetectionRequest(double? significanceThreshold, double[]? dispersionCoefficients)
    {
        SignificanceThreshold = significanceThreshold;
        DispersionCoefficients = dispersionCoefficients;
    }

    public double? SignificanceThreshold { get; }
    
    public double[]? DispersionCoefficients { get; }

    public static LineDetectionRequest Create(double? significanceThreshold = null, double[]? dispersionCoefficients = null)
    {
        if (significanceThreshold is { } threshold)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threshold);
        }

        return new LineDetectionRequest(significanceThreshold, dispersionCoefficients);
    }
}
