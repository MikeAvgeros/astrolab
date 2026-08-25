using AstroLab.Core.Imaging;

namespace AstroLab.Api.Features.Images.Render;

public sealed record RenderImageRequest(StretchMode Stretch = StretchMode.Asinh, ColorMap ColorMap = ColorMap.Grayscale, double? BlackPoint = null, double? WhitePoint = null, double LowerPercentile = RenderImageRequest.DefaultLowerPercentile, double UpperPercentile = RenderImageRequest.DefaultUpperPercentile, double AsinhSoftening = RenderImageRequest.DefaultAsinhSoftening)
{
    internal const double DefaultLowerPercentile = 1.0;
    internal const double DefaultUpperPercentile = 99.0;
    internal const double DefaultAsinhSoftening = 0.1;
}

/// <summary>Static factory accompanying <see cref="RenderImageRequest"/>. Validates arguments before constructing.</summary>
public static class RenderImageRequestFactory
{
    public static RenderImageRequest Create(StretchMode stretch = StretchMode.Asinh, ColorMap colorMap = ColorMap.Grayscale, double? blackPoint = null, double? whitePoint = null, double lowerPercentile = RenderImageRequest.DefaultLowerPercentile, double upperPercentile = RenderImageRequest.DefaultUpperPercentile, double asinhSoftening = RenderImageRequest.DefaultAsinhSoftening) =>
        new(stretch, colorMap, blackPoint, whitePoint, lowerPercentile, upperPercentile, asinhSoftening);
}
