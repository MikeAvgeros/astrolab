namespace AstroLab.Api.Features.Measurements.SpectralClassification;

public sealed record SpectralClassificationRequest
{
    private SpectralClassificationRequest(double? significanceThreshold)
    {
        SignificanceThreshold = significanceThreshold;
    }

    public double? SignificanceThreshold { get; }

    public static SpectralClassificationRequest Create(double? significanceThreshold = null)
    {
        if (significanceThreshold is { } threshold)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threshold);
        }

        return new SpectralClassificationRequest(significanceThreshold);
    }
}
