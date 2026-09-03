namespace AstroLab.Core.Imaging;

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

    public double BlackPoint { get; }

    public double WhitePoint { get; }

    public StretchMode Mode { get; }

    public double AsinhSoftening { get; }

    public double Range => WhitePoint - BlackPoint;

    public static ScaleParameters Create(double blackPoint, double whitePoint, StretchMode mode = StretchMode.Linear, double asinhSoftening = DefaultAsinhSoftening) =>
        new(blackPoint, whitePoint, mode, asinhSoftening);
}
