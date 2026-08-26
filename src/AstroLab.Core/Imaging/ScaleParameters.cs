namespace AstroLab.Core.Imaging;

/// <summary>
/// Parameters controlling the mapping from raw physical pixel values to a displayable [0, 1]
/// range: the black/white clipping points and the non-linear stretch applied within them.
/// </summary>
public readonly record struct ScaleParameters
{
    private const double DefaultAsinhSoftening = 0.1;

    private ScaleParameters(double blackPoint, double whitePoint, StretchMode mode, double asinhSoftening)
    {
        BlackPoint = blackPoint;
        WhitePoint = whitePoint;
        Mode = mode;
        AsinhSoftening = asinhSoftening;
    }

    /// <summary>The physical pixel value mapped to output 0.0 (and below).</summary>
    public double BlackPoint { get; }

    /// <summary>The physical pixel value mapped to output 1.0 (and above). Must exceed <see cref="BlackPoint"/>.</summary>
    public double WhitePoint { get; }

    /// <summary>The non-linear stretch applied after black/white-point clipping.</summary>
    public StretchMode Mode { get; }

    /// <summary>
    /// The softening parameter for <see cref="StretchMode.Asinh"/> (ignored otherwise). Smaller
    /// values increase contrast near the black point; typical values are in the range 0.01–0.5.
    /// </summary>
    public double AsinhSoftening { get; }

    public double Range => WhitePoint - BlackPoint;

    public static ScaleParameters Create(double blackPoint, double whitePoint, StretchMode mode = StretchMode.Linear, double asinhSoftening = DefaultAsinhSoftening) =>
        new(blackPoint, whitePoint, mode, asinhSoftening);
}
