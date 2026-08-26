namespace AstroLab.Api.Features.TimeSeries.Transit;

public sealed record TransitResponse
{
    private TransitResponse(string fileId, double bestPeriod, double transitDepth, double transitDuration)
    {
        FileId = fileId;
        BestPeriod = bestPeriod;
        TransitDepth = transitDepth;
        TransitDuration = transitDuration;
    }

    public string FileId { get; }

    public double BestPeriod { get; }

    public double TransitDepth { get; }

    public double TransitDuration { get; }

    public static TransitResponse Create(string fileId, double bestPeriod, double transitDepth, double transitDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new TransitResponse(fileId, bestPeriod, transitDepth, transitDuration);
    }
}
