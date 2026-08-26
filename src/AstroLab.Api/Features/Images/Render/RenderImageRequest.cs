using AstroLab.Core.Imaging;

namespace AstroLab.Api.Features.Images.Render;

public sealed record RenderImageRequest
{
    private const double DefaultLowerPercentile = 1.0;
    private const double DefaultUpperPercentile = 99.0;
    private const double DefaultAsinhSoftening = 0.1;

    public RenderImageRequest(StretchMode stretch = StretchMode.Asinh, ColorMap colorMap = ColorMap.Grayscale, double? blackPoint = null, double? whitePoint = null, double lowerPercentile = DefaultLowerPercentile, double upperPercentile = DefaultUpperPercentile, double asinhSoftening = DefaultAsinhSoftening)
    {
        Stretch = stretch;
        ColorMap = colorMap;
        BlackPoint = blackPoint;
        WhitePoint = whitePoint;
        LowerPercentile = lowerPercentile;
        UpperPercentile = upperPercentile;
        AsinhSoftening = asinhSoftening;
    }

    public StretchMode Stretch { get; }

    public ColorMap ColorMap { get; }

    public double? BlackPoint { get; }

    public double? WhitePoint { get; }

    public double LowerPercentile { get; }

    public double UpperPercentile { get; }

    public double AsinhSoftening { get; }

    public static RenderImageRequest Create(StretchMode stretch = StretchMode.Asinh, ColorMap colorMap = ColorMap.Grayscale, double? blackPoint = null, double? whitePoint = null, double lowerPercentile = DefaultLowerPercentile, double upperPercentile = DefaultUpperPercentile, double asinhSoftening = DefaultAsinhSoftening) =>
        new(stretch, colorMap, blackPoint, whitePoint, lowerPercentile, upperPercentile, asinhSoftening);
}
