namespace AstroLab.Core.Sources;

public readonly record struct SourceShape
{
    private SourceShape(int id, double semiMajorAxisPixels, double semiMinorAxisPixels, double ellipticity, double positionAngleDegrees)
    {
        Id = id;
        SemiMajorAxisPixels = semiMajorAxisPixels;
        SemiMinorAxisPixels = semiMinorAxisPixels;
        Ellipticity = ellipticity;
        PositionAngleDegrees = positionAngleDegrees;
    }

    public int Id { get; }

    public double SemiMajorAxisPixels { get; }

    public double SemiMinorAxisPixels { get; }

    public double Ellipticity { get; }

    public double PositionAngleDegrees { get; }

    public static SourceShape Create(int id, double semiMajorAxisPixels, double semiMinorAxisPixels, double ellipticity, double positionAngleDegrees)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        return new SourceShape(id, semiMajorAxisPixels, semiMinorAxisPixels, ellipticity, positionAngleDegrees);
    }
}
