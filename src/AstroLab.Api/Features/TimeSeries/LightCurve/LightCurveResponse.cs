using System.Collections.Immutable;

namespace AstroLab.Api.Features.TimeSeries.LightCurve;

public sealed record LightCurveResponse
{
    private LightCurveResponse(string fileId, ImmutableList<double> time, ImmutableList<double> flux)
    {
        FileId = fileId;
        Time = time;
        Flux = flux;
    }

    public string FileId { get; }

    public ImmutableList<double> Time { get; }

    public ImmutableList<double> Flux { get; }

    public static LightCurveResponse Create(string fileId, ImmutableList<double> time, ImmutableList<double> flux)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new LightCurveResponse(fileId, time, flux);
    }
}
