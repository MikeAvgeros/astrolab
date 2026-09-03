using AstroLab.Core.Imaging;
using AstroLab.Infrastructure.ImageRendering;

namespace AstroLab.Api.Features.Images.Render;

public sealed record RenderImageRequest
{
    internal const double DefaultLowerPercentile = 1.0;
    internal const double DefaultUpperPercentile = 99.0;
    internal const double DefaultAsinhSoftening = 0.1;

    private RenderImageRequest(StretchMode stretch = StretchMode.Asinh, ColorMap colorMap = ColorMap.Grayscale, 
        double? blackPoint = null, double? whitePoint = null, double lowerPercentile = DefaultLowerPercentile, 
        double upperPercentile = DefaultUpperPercentile, double asinhSoftening = DefaultAsinhSoftening, 
        int? maxDimension = RenderOptions.DefaultMaxDimension)
    {
        Stretch = stretch;
        ColorMap = colorMap;
        BlackPoint = blackPoint;
        WhitePoint = whitePoint;
        LowerPercentile = lowerPercentile;
        UpperPercentile = upperPercentile;
        AsinhSoftening = asinhSoftening;
        MaxDimension = maxDimension;
    }

    public StretchMode Stretch { get; }

    public ColorMap ColorMap { get; }

    public double? BlackPoint { get; }

    public double? WhitePoint { get; }

    public double LowerPercentile { get; }

    public double UpperPercentile { get; }

    public double AsinhSoftening { get; }
    
    public int? MaxDimension { get; }

    public static RenderImageRequest Create(StretchMode stretch = StretchMode.Asinh, ColorMap colorMap = ColorMap.Grayscale, 
        double? blackPoint = null, double? whitePoint = null, double lowerPercentile = DefaultLowerPercentile, 
        double upperPercentile = DefaultUpperPercentile, double asinhSoftening = DefaultAsinhSoftening, 
        int? maxDimension = RenderOptions.DefaultMaxDimension) =>
        new(stretch, colorMap, blackPoint, whitePoint, lowerPercentile, upperPercentile, asinhSoftening, maxDimension);
}
