namespace AstroLab.Api.Features.Images.Statistics;

public sealed record ImageStatisticsResponse(string FileId, double Min, double Max, double Mean, double StdDev, long ValidPixelCount, long TotalPixelCount, long InvalidPixelCount, double DeadPixelPercentage, double SkySigma);

/// <summary>Static factory accompanying <see cref="ImageStatisticsResponse"/>. Validates arguments before constructing.</summary>
public static class ImageStatisticsResponseFactory
{
    public static ImageStatisticsResponse Create(string fileId, double min, double max, double mean, double stdDev, long validPixelCount, long totalPixelCount, long invalidPixelCount, double deadPixelPercentage, double skySigma)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentOutOfRangeException.ThrowIfNegative(validPixelCount);

        ArgumentOutOfRangeException.ThrowIfNegative(totalPixelCount);

        ArgumentOutOfRangeException.ThrowIfNegative(invalidPixelCount);

        return new ImageStatisticsResponse(fileId, min, max, mean, stdDev, validPixelCount, totalPixelCount, invalidPixelCount, deadPixelPercentage, skySigma);
    }
}
