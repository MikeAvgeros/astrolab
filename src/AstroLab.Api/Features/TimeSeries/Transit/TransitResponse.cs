namespace AstroLab.Api.Features.TimeSeries.Transit;

public sealed record TransitResponse
{
    private TransitResponse(string fileId, double bestPeriod, double transitDepth, double transitDuration, double epoch)
    {
        FileId = fileId;
        BestPeriod = bestPeriod;
        TransitDepth = transitDepth;
        TransitDuration = transitDuration;
        Epoch = epoch;
    }

    public string FileId { get; }

    public double BestPeriod { get; }

    public double TransitDepth { get; }

    public double TransitDuration { get; }
    
    public double Epoch { get; }

    public static TransitResponse Create(string fileId, double bestPeriod, double transitDepth, double transitDuration, double epoch)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new TransitResponse(fileId, bestPeriod, transitDepth, transitDuration, epoch);
    }
}
