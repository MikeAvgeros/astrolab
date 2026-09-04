namespace AstroLab.Api.Features.Images.SourceCharacterization;

public sealed record SourceShapeDto
{
    private SourceShapeDto(int sourceId, double semiMajorAxisPixels, double semiMinorAxisPixels, double ellipticity, double positionAngleDegrees)
    {
        SourceId = sourceId;
        SemiMajorAxisPixels = semiMajorAxisPixels;
        SemiMinorAxisPixels = semiMinorAxisPixels;
        Ellipticity = ellipticity;
        PositionAngleDegrees = positionAngleDegrees;
    }

    public int SourceId { get; }

    public double SemiMajorAxisPixels { get; }

    public double SemiMinorAxisPixels { get; }

    public double Ellipticity { get; }

    public double PositionAngleDegrees { get; }

    public static SourceShapeDto Create(int sourceId, double semiMajorAxisPixels, double semiMinorAxisPixels, double ellipticity, double positionAngleDegrees)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sourceId);

        return new SourceShapeDto(sourceId, semiMajorAxisPixels, semiMinorAxisPixels, ellipticity, positionAngleDegrees);
    }
}
