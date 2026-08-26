namespace AstroLab.Api.Features.Images.Statistics;

public sealed record ImageStatisticsResponse
{
    private ImageStatisticsResponse(string fileId, double min, double max, double mean, double stdDev, long validPixelCount, long totalPixelCount, long invalidPixelCount, double deadPixelPercentage, double skySigma)
    {
        FileId = fileId;
        Min = min;
        Max = max;
        Mean = mean;
        StdDev = stdDev;
        ValidPixelCount = validPixelCount;
        TotalPixelCount = totalPixelCount;
        InvalidPixelCount = invalidPixelCount;
        DeadPixelPercentage = deadPixelPercentage;
        SkySigma = skySigma;
    }

    public string FileId { get; }

    public double Min { get; }

    public double Max { get; }

    public double Mean { get; }

    public double StdDev { get; }

    public long ValidPixelCount { get; }

    public long TotalPixelCount { get; }

    public long InvalidPixelCount { get; }

    public double DeadPixelPercentage { get; }

    public double SkySigma { get; }

    public static ImageStatisticsResponse Create(string fileId, double min, double max, double mean, double stdDev, long validPixelCount, long totalPixelCount, long invalidPixelCount, double deadPixelPercentage, double skySigma)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentOutOfRangeException.ThrowIfNegative(validPixelCount);

        ArgumentOutOfRangeException.ThrowIfNegative(totalPixelCount);

        ArgumentOutOfRangeException.ThrowIfNegative(invalidPixelCount);

        return new ImageStatisticsResponse(fileId, min, max, mean, stdDev, validPixelCount, totalPixelCount, invalidPixelCount, deadPixelPercentage, skySigma);
    }
}
