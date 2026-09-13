using System.Collections.Immutable;

namespace AstroLab.Api.Features.TimeSeries.PhaseFold;

public sealed record PhaseFoldResponse
{
    private PhaseFoldResponse(string fileId, ImmutableList<double> time, ImmutableList<double> phase, ImmutableList<double> flux)
    {
        FileId = fileId;
        Time = time;
        Phase = phase;
        Flux = flux;
    }

    public string FileId { get; }

    public ImmutableList<double> Time { get; }

    public ImmutableList<double> Phase { get; }

    public ImmutableList<double> Flux { get; }

    public static PhaseFoldResponse Create(string fileId, ImmutableList<double> time, ImmutableList<double> phase, ImmutableList<double> flux)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new PhaseFoldResponse(fileId, time, phase, flux);
    }
}
