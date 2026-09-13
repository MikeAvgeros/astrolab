namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record PixelWorldPairDto
{
    private PixelWorldPairDto(double pixelX, double pixelY, double rightAscension, double declination)
    {
        PixelX = pixelX;
        PixelY = pixelY;
        RightAscension = rightAscension;
        Declination = declination;
    }

    public double PixelX { get; }

    public double PixelY { get; }

    public double RightAscension { get; }

    public double Declination { get; }

    public static PixelWorldPairDto Create(double pixelX, double pixelY, double rightAscension, double declination) =>
        new(pixelX, pixelY, rightAscension, declination);
}
