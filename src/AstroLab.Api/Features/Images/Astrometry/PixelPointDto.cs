namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record PixelPointDto
{
    private PixelPointDto(double pixelX, double pixelY)
    {
        PixelX = pixelX;
        PixelY = pixelY;
    }

    public double PixelX { get; }

    public double PixelY { get; }

    public static PixelPointDto Create(double pixelX, double pixelY) =>
        new(pixelX, pixelY);
}
