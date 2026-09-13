using System.Collections.Immutable;

namespace AstroLab.Core.Imaging;

public readonly record struct ImageQualityReport
{
    private ImageQualityReport(
        long nanCount, long infiniteCount, long invalidPixelCount, long validPixelCount, long totalPixelCount,
        double min, double max, double mean, double stdDev,
        double estimatedBackground, double estimatedNoise, double? dynamicRange,
        double? saturationThreshold, bool saturationThresholdFromHeader, long? saturatedPixelCount, double? saturatedPixelFraction,
        double usablePixelFraction, ImmutableArray<string> qualityFlags)
    {
        NanCount = nanCount;
        InfiniteCount = infiniteCount;
        InvalidPixelCount = invalidPixelCount;
        ValidPixelCount = validPixelCount;
        TotalPixelCount = totalPixelCount;
        Min = min;
        Max = max;
        Mean = mean;
        StdDev = stdDev;
        EstimatedBackground = estimatedBackground;
        EstimatedNoise = estimatedNoise;
        DynamicRange = dynamicRange;
        SaturationThreshold = saturationThreshold;
        SaturationThresholdFromHeader = saturationThresholdFromHeader;
        SaturatedPixelCount = saturatedPixelCount;
        SaturatedPixelFraction = saturatedPixelFraction;
        UsablePixelFraction = usablePixelFraction;
        QualityFlags = qualityFlags;
    }

    public long NanCount { get; }

    public long InfiniteCount { get; }

    public long InvalidPixelCount { get; }

    public long ValidPixelCount { get; }

    public long TotalPixelCount { get; }

    public double Min { get; }

    public double Max { get; }

    public double Mean { get; }

    public double StdDev { get; }

    public double EstimatedBackground { get; }

    public double EstimatedNoise { get; }

    public double? DynamicRange { get; }

    public double? SaturationThreshold { get; }

    public bool SaturationThresholdFromHeader { get; }

    public long? SaturatedPixelCount { get; }

    public double? SaturatedPixelFraction { get; }

    public double UsablePixelFraction { get; }

    public ImmutableArray<string> QualityFlags { get; }

    public static ImageQualityReport Create(
        long nanCount, long infiniteCount, long invalidPixelCount, long validPixelCount, long totalPixelCount,
        double min, double max, double mean, double stdDev,
        double estimatedBackground, double estimatedNoise, double? dynamicRange,
        double? saturationThreshold, bool saturationThresholdFromHeader, long? saturatedPixelCount, double? saturatedPixelFraction,
        double usablePixelFraction, ImmutableArray<string> qualityFlags)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(nanCount);

        ArgumentOutOfRangeException.ThrowIfNegative(infiniteCount);

        ArgumentOutOfRangeException.ThrowIfNegative(validPixelCount);

        ArgumentOutOfRangeException.ThrowIfNegative(totalPixelCount);

        return new ImageQualityReport(
            nanCount, infiniteCount, invalidPixelCount, validPixelCount, totalPixelCount,
            min, max, mean, stdDev, estimatedBackground, estimatedNoise, dynamicRange,
            saturationThreshold, saturationThresholdFromHeader, saturatedPixelCount, saturatedPixelFraction,
            usablePixelFraction, qualityFlags);
    }
}
