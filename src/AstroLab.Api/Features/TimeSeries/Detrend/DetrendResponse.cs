using System.Collections.Immutable;

namespace AstroLab.Api.Features.TimeSeries.Detrend;

public sealed record DetrendResponse
{
    private DetrendResponse(string fileId, ImmutableList<double> time, ImmutableList<double> detrendedFlux)
    {
        FileId = fileId;
        Time = time;
        DetrendedFlux = detrendedFlux;
    }

    public string FileId { get; }

    public ImmutableList<double> Time { get; }

    public ImmutableList<double> DetrendedFlux { get; }

    public static DetrendResponse Create(string fileId, ImmutableList<double> time, ImmutableList<double> detrendedFlux)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new DetrendResponse(fileId, time, detrendedFlux);
    }
}
