namespace AstroLab.Api.Features.Measurements.GalaxyMorphology;

public sealed record GalaxyMorphologyRequest
{
    private GalaxyMorphologyRequest(double centerX, double centerY)
    {
        CenterX = centerX;
        CenterY = centerY;
    }

    public double CenterX { get; }

    public double CenterY { get; }

    public static GalaxyMorphologyRequest Create(double centerX, double centerY) =>
        new(centerX, centerY);
}
