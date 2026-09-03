namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record WorldToPixelRequest
{
    public WorldToPixelRequest(double rightAscension, double declination)
    {
        RightAscension = rightAscension;
        Declination = declination;
    }

    public double RightAscension { get; }

    public double Declination { get; }

    public static WorldToPixelRequest Create(double rightAscension, double declination) =>
        new(rightAscension, declination);
}
