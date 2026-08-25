namespace AstroLab.Api.Features.Images.Sources;

public sealed record DetectedSourceDto(double X, double Y, double Flux);

/// <summary>Static factory accompanying <see cref="DetectedSourceDto"/>.</summary>
public static class DetectedSourceDtoFactory
{
    public static DetectedSourceDto Create(double x, double y, double flux) =>
        new(x, y, flux);
}
