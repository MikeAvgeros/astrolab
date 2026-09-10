namespace AstroLab.Core.Sources;

public readonly record struct ImageSegment
{
    private ImageSegment(int segmentId, int pixelCount, double centroidX, double centroidY, int minX, int minY, int maxX, int maxY)
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

    public static ImageSegment Create(int segmentId, int pixelCount, double centroidX, double centroidY, int minX, int minY, int maxX, int maxY)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segmentId);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelCount);

        return new ImageSegment(segmentId, pixelCount, centroidX, centroidY, minX, minY, maxX, maxY);
    }
}
