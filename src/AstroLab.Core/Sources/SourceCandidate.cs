namespace AstroLab.Core.Sources;

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
