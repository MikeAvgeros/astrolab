namespace AstroLab.Api.Features.Images.Segmentation;

public sealed record SegmentDto
{
    private SegmentDto(int segmentId, int pixelCount, double centroidX, double centroidY, int minX, int minY, int maxX, int maxY)
    {
        SegmentId = segmentId;
        PixelCount = pixelCount;
        CentroidX = centroidX;
        CentroidY = centroidY;
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }

    public int SegmentId { get; }

    public int PixelCount { get; }

    public double CentroidX { get; }

    public double CentroidY { get; }

    public int MinX { get; }

    public int MinY { get; }

    public int MaxX { get; }

    public int MaxY { get; }

    public static SegmentDto Create(int segmentId, int pixelCount, double centroidX, double centroidY, int minX, int minY, int maxX, int maxY)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(segmentId);

        ArgumentOutOfRangeException.ThrowIfNegative(pixelCount);

        return new SegmentDto(segmentId, pixelCount, centroidX, centroidY, minX, minY, maxX, maxY);
    }
}
