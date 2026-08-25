using System.Collections.Immutable;

namespace AstroLab.Api.Features.TimeSeries.Detrend;

public sealed record DetrendResponse(string FileId, ImmutableList<double> Time, ImmutableList<double> DetrendedFlux);

/// <summary>Static factory accompanying <see cref="DetrendResponse"/>. Validates arguments before constructing.</summary>
public static class DetrendResponseFactory
{
    public static DetrendResponse Create(string fileId, ImmutableList<double> time, ImmutableList<double> detrendedFlux)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new DetrendResponse(fileId, time, detrendedFlux);
    }
}
