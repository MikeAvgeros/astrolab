namespace AstroLab.Core.Astrometry;

public readonly record struct AlignmentTransform
{
    private AlignmentTransform(double offsetX, double offsetY, double rotationDegrees, double scale)
    {
        OffsetX = offsetX;
        OffsetY = offsetY;
        RotationDegrees = rotationDegrees;
        Scale = scale;
    }

    public double OffsetX { get; }

    public double OffsetY { get; }

    public double RotationDegrees { get; }

    public double Scale { get; }

    public static AlignmentTransform Create(double offsetX, double offsetY, double rotationDegrees, double scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scale);

        return new AlignmentTransform(offsetX, offsetY, rotationDegrees, scale);
    }
}
