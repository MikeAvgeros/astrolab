namespace AstroLab.Api.Features.Images.Footprint;

public sealed record WorldPointDto
{
    private WorldPointDto(double rightAscension, double declination)
    {
        RightAscension = rightAscension;
        Declination = declination;
    }

    public double RightAscension { get; }

    public double Declination { get; }

    public static WorldPointDto Create(double rightAscension, double declination) =>
        new(rightAscension, declination);
}
