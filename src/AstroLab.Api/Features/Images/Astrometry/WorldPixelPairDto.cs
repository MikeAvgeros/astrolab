namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record WorldPixelPairDto
{
    private WorldPixelPairDto(double rightAscension, double declination, double pixelX, double pixelY)
    {
        RightAscension = rightAscension;
        Declination = declination;
        PixelX = pixelX;
        PixelY = pixelY;
    }

    public double RightAscension { get; }

    public double Declination { get; }

    public double PixelX { get; }

    public double PixelY { get; }

    public static WorldPixelPairDto Create(double rightAscension, double declination, double pixelX, double pixelY) =>
        new(rightAscension, declination, pixelX, pixelY);
}
