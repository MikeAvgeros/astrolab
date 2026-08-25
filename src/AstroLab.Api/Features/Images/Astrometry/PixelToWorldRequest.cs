namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record PixelToWorldRequest(double PixelX, double PixelY);

/// <summary>Static factory accompanying <see cref="PixelToWorldRequest"/>.</summary>
public static class PixelToWorldRequestFactory
{
    public static PixelToWorldRequest Create(double pixelX, double pixelY) =>
        new(pixelX, pixelY);
}
