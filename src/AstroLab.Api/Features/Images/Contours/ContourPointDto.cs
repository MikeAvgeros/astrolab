namespace AstroLab.Api.Features.Images.Contours;

public sealed record ContourPointDto
{
    private ContourPointDto(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }

    public double Y { get; }

    public static ContourPointDto Create(double x, double y) => new(x, y);
}
