using AstroLab.Core.Imaging;

namespace AstroLab.Infrastructure.ImageRendering;

/// <summary>
/// Options controlling how a raw FITS pixel array is turned into a displayable image.
/// </summary>
/// <param name="Stretch">The non-linear intensity transform applied within the black/white points.</param>
/// <param name="AsinhSoftening">Softening parameter used only when <paramref name="Stretch"/> is <see cref="StretchMode.Asinh"/>.</param>
/// <param name="ColorMap">The palette applied to the stretched grayscale intensity.</param>
/// <param name="BlackPoint">
/// The physical pixel value mapped to black. When <see langword="null"/>, it is computed
/// automatically from <paramref name="AutoLowerPercentile"/>.
/// </param>
/// <param name="WhitePoint">
/// The physical pixel value mapped to white. When <see langword="null"/>, it is computed
/// automatically from <paramref name="AutoUpperPercentile"/>.
/// </param>
/// <param name="AutoLowerPercentile">Lower percentile used to derive <paramref name="BlackPoint"/> when it is not supplied.</param>
/// <param name="AutoUpperPercentile">Upper percentile used to derive <paramref name="WhitePoint"/> when it is not supplied.</param>
public readonly record struct RenderOptions(
    StretchMode Stretch = StretchMode.Asinh,
    double AsinhSoftening = 0.1,
    ColorMap ColorMap = ColorMap.Grayscale,
    double? BlackPoint = null,
    double? WhitePoint = null,
    double AutoLowerPercentile = 1.0,
    double AutoUpperPercentile = 99.0)
{
    public bool RequiresAutoScale => BlackPoint is null || WhitePoint is null;
}
