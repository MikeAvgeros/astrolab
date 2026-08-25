using System.Collections.Immutable;

namespace AstroLab.Api.Features.TimeSeries.LightCurve;

public sealed record LightCurveResponse(string FileId, ImmutableList<double> Time, ImmutableList<double> Flux);

/// <summary>Static factory accompanying <see cref="LightCurveResponse"/>. Validates arguments before constructing.</summary>
public static class LightCurveResponseFactory
{
    public static LightCurveResponse Create(string fileId, ImmutableList<double> time, ImmutableList<double> flux)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new LightCurveResponse(fileId, time, flux);
    }
}
