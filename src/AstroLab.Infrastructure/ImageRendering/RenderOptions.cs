using AstroLab.Core.Imaging;

namespace AstroLab.Infrastructure.ImageRendering;

public readonly record struct RenderOptions
{
    private const double DefaultAsinhSoftening = 0.1;
    private const double DefaultAutoLowerPercentile = 1.0;
    private const double DefaultAutoUpperPercentile = 99.0;

    public const int DefaultMaxDimension = 4096;

    private RenderOptions(
        StretchMode stretch,
        double asinhSoftening,
        ColorMap colorMap,
        double? blackPoint,
        double? whitePoint,
        double autoLowerPercentile,
        double autoUpperPercentile,
        int? maxDimension)
    {
        Stretch = stretch;
        AsinhSoftening = asinhSoftening;
        ColorMap = colorMap;
        BlackPoint = blackPoint;
        WhitePoint = whitePoint;
        AutoLowerPercentile = autoLowerPercentile;
        AutoUpperPercentile = autoUpperPercentile;
        MaxDimension = maxDimension;
    }

    public StretchMode Stretch { get; }

    public double AsinhSoftening { get; }

    public ColorMap ColorMap { get; }

    public double? BlackPoint { get; }

    public double? WhitePoint { get; }

    public double AutoLowerPercentile { get; }

    public double AutoUpperPercentile { get; }

    public int? MaxDimension { get; }

    public bool RequiresAutoScale => BlackPoint is null || WhitePoint is null;

    public static RenderOptions Create(
        StretchMode stretch = StretchMode.Asinh,
        double asinhSoftening = DefaultAsinhSoftening,
        ColorMap colorMap = ColorMap.Grayscale,
        double? blackPoint = null,
        double? whitePoint = null,
        double autoLowerPercentile = DefaultAutoLowerPercentile,
        double autoUpperPercentile = DefaultAutoUpperPercentile,
        int? maxDimension = DefaultMaxDimension) =>
        new(stretch, asinhSoftening, colorMap, blackPoint, whitePoint, autoLowerPercentile, autoUpperPercentile, maxDimension);
}
