using System.Collections.Immutable;
using AstroLab.Core.Imaging;

namespace AstroLab.Api.Features.Fits.Quality;

public sealed record FitsQualityResponse
{
    private FitsQualityResponse(
        string fileId, long nanCount, long infiniteCount, long invalidPixelCount, long validPixelCount, long totalPixelCount,
        double min, double max, double mean, double stdDev,
        double estimatedBackground, double estimatedNoise, double? dynamicRange,
        double? saturationThreshold, bool saturationThresholdFromHeader, long? saturatedPixelCount, double? saturatedPixelFraction,
        double usablePixelFraction, ImmutableList<string> qualityFlags)
    {
        FileId = fileId;
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

    public string FileId { get; }

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

    public ImmutableList<string> QualityFlags { get; }

    public static FitsQualityResponse Create(string fileId, ImageQualityReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new FitsQualityResponse(
            fileId, report.NanCount, report.InfiniteCount, report.InvalidPixelCount, report.ValidPixelCount, report.TotalPixelCount,
            report.Min, report.Max, report.Mean, report.StdDev,
            report.EstimatedBackground, report.EstimatedNoise, report.DynamicRange,
            report.SaturationThreshold, report.SaturationThresholdFromHeader, report.SaturatedPixelCount, report.SaturatedPixelFraction,
            report.UsablePixelFraction, [.. report.QualityFlags]);
    }
}
