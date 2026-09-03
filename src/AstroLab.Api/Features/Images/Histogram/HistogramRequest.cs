using AstroLab.Core.Imaging;

namespace AstroLab.Api.Features.Images.Histogram;

public sealed record HistogramRequest
{
    public HistogramRequest(int binCount = ImageStatistics.DefaultDisplayHistogramBinCount)
    {
        BinCount = binCount;
    }

    public int BinCount { get; }

    public static HistogramRequest Create(int binCount = ImageStatistics.DefaultDisplayHistogramBinCount) => new(binCount);
}
