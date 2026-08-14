namespace AstroLab.Api.Features.Imaging;

public sealed record ImageStatisticsResponse(
    string FileId, double Min, double Max, double Mean, double StdDev, long ValidPixelCount, long TotalPixelCount);
