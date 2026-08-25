namespace AstroLab.Api.Features.Spectroscopy.Lines;

public sealed record LineDetectionRequest(double? SignificanceThreshold = null);

/// <summary>Static factory accompanying <see cref="LineDetectionRequest"/>. Validates arguments before constructing.</summary>
public static class LineDetectionRequestFactory
{
    public static LineDetectionRequest Create(double? significanceThreshold = null)
    {
        if (significanceThreshold is { } threshold)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threshold);
        }

        return new LineDetectionRequest(significanceThreshold);
    }
}
