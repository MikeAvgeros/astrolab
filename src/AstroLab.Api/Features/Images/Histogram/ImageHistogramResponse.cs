using System.Collections.Immutable;
using AstroLab.Core.Imaging;

namespace AstroLab.Api.Features.Images.Histogram;

public sealed record ImageHistogramResponse
{
    private ImageHistogramResponse(string fileId, ImmutableList<double> binEdges, ImmutableList<long> counts, int binCount, long validPixelCount)
    {
        FileId = fileId;
        BinEdges = binEdges;
        Counts = counts;
        BinCount = binCount;
        ValidPixelCount = validPixelCount;
    }

    public string FileId { get; }

    public ImmutableList<double> BinEdges { get; }

    public ImmutableList<long> Counts { get; }

    public int BinCount { get; }

    public long ValidPixelCount { get; }

    public static ImageHistogramResponse Create(string fileId, ImmutableList<double> binEdges, ImmutableList<long> counts, int binCount, long validPixelCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentOutOfRangeException.ThrowIfNegative(binCount);

        ArgumentOutOfRangeException.ThrowIfNegative(validPixelCount);

        return new ImageHistogramResponse(fileId, binEdges, counts, binCount, validPixelCount);
    }

    public static ImageHistogramResponse FromHistogram(string fileId, ImageHistogram histogram) =>
        Create(fileId, histogram.BinEdges.ToImmutableList(), histogram.Counts.ToImmutableList(), histogram.BinCount, histogram.ValidPixelCount);
}
