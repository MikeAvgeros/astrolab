using System.Collections.Immutable;

namespace AstroLab.Api.Features.Spectroscopy.Snr;

public sealed record SnrResponse
{
    private SnrResponse(string fileId, double overallSnr, ImmutableList<double> perSampleSnr)
    {
        FileId = fileId;
        OverallSnr = overallSnr;
        PerSampleSnr = perSampleSnr;
    }

    public string FileId { get; }

    public double OverallSnr { get; }

    public ImmutableList<double> PerSampleSnr { get; }

    public static SnrResponse Create(string fileId, double overallSnr, ImmutableList<double> perSampleSnr)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new SnrResponse(fileId, overallSnr, perSampleSnr);
    }
}
