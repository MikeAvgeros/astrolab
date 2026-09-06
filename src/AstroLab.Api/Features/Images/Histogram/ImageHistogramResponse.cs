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

    public static ImageHistogramResponse Create(string fileId, ImageHistogram histogram)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);
        
        ArgumentOutOfRangeException.ThrowIfNegative(histogram.BinCount);
        
        ArgumentOutOfRangeException.ThrowIfNegative(histogram.ValidPixelCount);

        return new ImageHistogramResponse(
            fileId,
            histogram.BinEdges.ToImmutableList(),
            histogram.Counts.ToImmutableList(),
            histogram.BinCount,
            histogram.ValidPixelCount);
    }
}
