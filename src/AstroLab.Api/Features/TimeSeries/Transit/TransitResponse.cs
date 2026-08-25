namespace AstroLab.Api.Features.TimeSeries.Transit;

public sealed record TransitResponse(string FileId, double BestPeriod, double TransitDepth, double TransitDuration);

/// <summary>Static factory accompanying <see cref="TransitResponse"/>. Validates arguments before constructing.</summary>
public static class TransitResponseFactory
{
    public static TransitResponse Create(string fileId, double bestPeriod, double transitDepth, double transitDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new TransitResponse(fileId, bestPeriod, transitDepth, transitDuration);
    }
}
