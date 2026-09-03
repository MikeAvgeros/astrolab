namespace AstroLab.Core.Sources;

/// <summary>Accumulated pixel statistics for one connected region discovered during flood-fill labeling, before the minimum-area and max-sources filters are applied.</summary>
internal readonly record struct SourceCandidate
{
    private SourceCandidate(int firstPixelIndex, int pixelCount, double peakValue, double totalFlux, double weightedXSum, double weightedYSum, double weightSum)
    {
        FirstPixelIndex = firstPixelIndex;
        PixelCount = pixelCount;
        PeakValue = peakValue;
        TotalFlux = totalFlux;
        WeightedXSum = weightedXSum;
        WeightedYSum = weightedYSum;
        WeightSum = weightSum;
    }

    /// <summary>The raster-scan pixel index where this region's flood fill started — a fixed, deterministic tie-breaker for sorting equal-flux candidates.</summary>
    public int FirstPixelIndex { get; }

    public int PixelCount { get; }

    public double PeakValue { get; }

    public double TotalFlux { get; }

    public double WeightedXSum { get; }

    public double WeightedYSum { get; }

    public double WeightSum { get; }

    public static SourceCandidate Create(int firstPixelIndex, int pixelCount, double peakValue, double totalFlux, double weightedXSum, double weightedYSum, double weightSum) =>
        new(firstPixelIndex, pixelCount, peakValue, totalFlux, weightedXSum, weightedYSum, weightSum);
}
