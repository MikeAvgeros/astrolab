namespace AstroLab.Api.Features.Spectroscopy.Lines;

public sealed record LineDetectionRequest
{
    public LineDetectionRequest(double? significanceThreshold = null)
    {
        SignificanceThreshold = significanceThreshold;
    }

    public double? SignificanceThreshold { get; }

    public static LineDetectionRequest Create(double? significanceThreshold = null)
    {
        if (significanceThreshold is { } threshold)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threshold);
        }

        return new LineDetectionRequest(significanceThreshold);
    }
}
