using AstroLab.Core.Imaging;

namespace AstroLab.Infrastructure.ImageRendering;

/// <summary>
/// Options controlling how a raw FITS pixel array is turned into a displayable image.
/// </summary>
public readonly record struct RenderOptions
{
    private const double DefaultAsinhSoftening = 0.1;
    private const double DefaultAutoLowerPercentile = 1.0;
    private const double DefaultAutoUpperPercentile = 99.0;

    private RenderOptions(
        StretchMode stretch,
        double asinhSoftening,
        ColorMap colorMap,
        double? blackPoint,
        double? whitePoint,
        double autoLowerPercentile,
        double autoUpperPercentile)
    {
        Stretch = stretch;
        AsinhSoftening = asinhSoftening;
        ColorMap = colorMap;
        BlackPoint = blackPoint;
        WhitePoint = whitePoint;
        AutoLowerPercentile = autoLowerPercentile;
        AutoUpperPercentile = autoUpperPercentile;
    }

    /// <summary>The non-linear intensity transform applied within the black/white points.</summary>
    public StretchMode Stretch { get; }

    /// <summary>Softening parameter used only when <see cref="Stretch"/> is <see cref="StretchMode.Asinh"/>.</summary>
    public double AsinhSoftening { get; }

    /// <summary>The palette applied to the stretched grayscale intensity.</summary>
    public ColorMap ColorMap { get; }

    /// <summary>
    /// The physical pixel value mapped to black. When <see langword="null"/>, it is computed
    /// automatically from <see cref="AutoLowerPercentile"/>.
    /// </summary>
    public double? BlackPoint { get; }

    /// <summary>
    /// The physical pixel value mapped to white. When <see langword="null"/>, it is computed
    /// automatically from <see cref="AutoUpperPercentile"/>.
    /// </summary>
    public double? WhitePoint { get; }

    /// <summary>Lower percentile used to derive <see cref="BlackPoint"/> when it is not supplied.</summary>
    public double AutoLowerPercentile { get; }

    /// <summary>Upper percentile used to derive <see cref="WhitePoint"/> when it is not supplied.</summary>
    public double AutoUpperPercentile { get; }

    public bool RequiresAutoScale => BlackPoint is null || WhitePoint is null;

    public static RenderOptions Create(
        StretchMode stretch = StretchMode.Asinh,
        double asinhSoftening = DefaultAsinhSoftening,
        ColorMap colorMap = ColorMap.Grayscale,
        double? blackPoint = null,
        double? whitePoint = null,
        double autoLowerPercentile = DefaultAutoLowerPercentile,
        double autoUpperPercentile = DefaultAutoUpperPercentile) =>
        new(stretch, asinhSoftening, colorMap, blackPoint, whitePoint, autoLowerPercentile, autoUpperPercentile);
}
