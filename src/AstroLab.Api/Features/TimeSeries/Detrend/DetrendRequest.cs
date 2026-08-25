namespace AstroLab.Api.Features.TimeSeries.Detrend;

public sealed record DetrendRequest(string Method);

/// <summary>Static factory accompanying <see cref="DetrendRequest"/>. Validates arguments before constructing.</summary>
public static class DetrendRequestFactory
{
    public static DetrendRequest Create(string method)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        return new DetrendRequest(method);
    }
}
