using System.Collections.Immutable;

namespace AstroLab.Api.Features.TimeSeries.Compare;

public sealed record LightCurveCompareResponse
{
    private LightCurveCompareResponse(string fileId, ImmutableList<LightCurveComparisonEntry> comparisons)
    {
        FileId = fileId;
        Comparisons = comparisons;
    }

    public string FileId { get; }

    public ImmutableList<LightCurveComparisonEntry> Comparisons { get; }

    public static LightCurveCompareResponse Create(string fileId, ImmutableList<LightCurveComparisonEntry> comparisons)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new LightCurveCompareResponse(fileId, comparisons);
    }
}
