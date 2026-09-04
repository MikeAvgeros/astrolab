namespace AstroLab.Api.Features.Images.Separation;

public sealed record AngularSeparationRequest
{
    private AngularSeparationRequest(double firstPixelX, double firstPixelY, double secondPixelX, double secondPixelY)
    {
        FirstPixelX = firstPixelX;
        FirstPixelY = firstPixelY;
        SecondPixelX = secondPixelX;
        SecondPixelY = secondPixelY;
    }

    public double FirstPixelX { get; }

    public double FirstPixelY { get; }

    public double SecondPixelX { get; }

    public double SecondPixelY { get; }

    public static AngularSeparationRequest Create(double firstPixelX, double firstPixelY, double secondPixelX, double secondPixelY) =>
        new(firstPixelX, firstPixelY, secondPixelX, secondPixelY);
}
