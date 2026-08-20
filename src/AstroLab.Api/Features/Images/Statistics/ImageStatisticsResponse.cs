namespace AstroLab.Api.Features.Images.Statistics;

public sealed record ImageStatisticsResponse(
    string FileId,
    double Min,
    double Max,
    double Mean,
    double StdDev,
    long ValidPixelCount,
    long TotalPixelCount,
    long InvalidPixelCount,
    double DeadPixelPercentage,
    double SkySigma);
