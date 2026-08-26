namespace AstroLab.Api.Features.Images.Sources;

public sealed record DetectedSourceDto
{
    private DetectedSourceDto(double x, double y, double flux)
    {
        X = x;
        Y = y;
        Flux = flux;
    }

    public double X { get; }

    public double Y { get; }

    public double Flux { get; }

    public static DetectedSourceDto Create(double x, double y, double flux) =>
        new(x, y, flux);
}
