namespace AstroLab.Api.Features.Measurements.SurfaceBrightness;

public sealed record SurfaceBrightnessRequest
{
    private SurfaceBrightnessRequest(double centerX, double centerY, double apertureRadius)
    {
        CenterX = centerX;
        CenterY = centerY;
        ApertureRadius = apertureRadius;
    }

    public double CenterX { get; }

    public double CenterY { get; }

    public double ApertureRadius { get; }

    public static SurfaceBrightnessRequest Create(double centerX, double centerY, double apertureRadius)
    {
        var request = new SurfaceBrightnessRequest(centerX, centerY, apertureRadius);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ApertureRadius);
    }
}
