namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record PixelToWorldRequest
{
    private PixelToWorldRequest(double pixelX, double pixelY)
    {
        PixelX = pixelX;
        PixelY = pixelY;
    }

    public double PixelX { get; }

    public double PixelY { get; }

    public static PixelToWorldRequest Create(double pixelX, double pixelY) =>
        new(pixelX, pixelY);
}
