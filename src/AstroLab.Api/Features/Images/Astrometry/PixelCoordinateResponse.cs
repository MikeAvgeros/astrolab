namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record PixelCoordinateResponse
{
    private PixelCoordinateResponse(string fileId, double pixelX, double pixelY)
    {
        FileId = fileId;
        PixelX = pixelX;
        PixelY = pixelY;
    }

    public string FileId { get; }

    public double PixelX { get; }

    public double PixelY { get; }

    public static PixelCoordinateResponse Create(string fileId, double pixelX, double pixelY)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new PixelCoordinateResponse(fileId, pixelX, pixelY);
    }
}
