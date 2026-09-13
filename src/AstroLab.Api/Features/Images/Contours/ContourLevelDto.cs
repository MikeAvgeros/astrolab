using System.Collections.Immutable;

namespace AstroLab.Api.Features.Images.Contours;

public sealed record ContourLevelDto
{
    private ContourLevelDto(double level, ImmutableList<ImmutableList<ContourPointDto>> polylines)
    {
        Level = level;
        Polylines = polylines;
    }

    public double Level { get; }

    public ImmutableList<ImmutableList<ContourPointDto>> Polylines { get; }

    public static ContourLevelDto Create(double level, ImmutableList<ImmutableList<ContourPointDto>> polylines) =>
        new(level, polylines);
}
