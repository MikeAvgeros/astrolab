namespace AstroLab.Core.Imaging;

/// <summary>
/// Parameters controlling the mapping from raw physical pixel values to a displayable [0, 1]
/// range: the black/white clipping points and the non-linear stretch applied within them.
/// </summary>
/// <param name="BlackPoint">The physical pixel value mapped to output 0.0 (and below).</param>
/// <param name="WhitePoint">The physical pixel value mapped to output 1.0 (and above). Must exceed <see cref="BlackPoint"/>.</param>
/// <param name="Mode">The non-linear stretch applied after black/white-point clipping.</param>
/// <param name="AsinhSoftening">
/// The softening parameter for <see cref="StretchMode.Asinh"/> (ignored otherwise). Smaller
/// values increase contrast near the black point; typical values are in the range 0.01–0.5.
/// </param>
public readonly record struct ScaleParameters(double BlackPoint, double WhitePoint, StretchMode Mode = StretchMode.Linear, double AsinhSoftening = ScaleParametersFactory.DefaultAsinhSoftening)
{
    public double Range => WhitePoint - BlackPoint;
}

/// <summary>Static factory accompanying <see cref="ScaleParameters"/>.</summary>
public static class ScaleParametersFactory
{
    public const double DefaultAsinhSoftening = 0.1;

    public static ScaleParameters Create(double blackPoint, double whitePoint, StretchMode mode = StretchMode.Linear, double asinhSoftening = DefaultAsinhSoftening) =>
        new(blackPoint, whitePoint, mode, asinhSoftening);
}
