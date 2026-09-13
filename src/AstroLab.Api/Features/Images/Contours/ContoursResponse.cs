using System.Collections.Immutable;

namespace AstroLab.Api.Features.Images.Contours;

public sealed record ContoursResponse
{
    private ContoursResponse(string fileId, ImmutableList<ContourLevelDto> levels)
    {
        FileId = fileId;
        Levels = levels;
    }

    public string FileId { get; }

    public ImmutableList<ContourLevelDto> Levels { get; }

    public static ContoursResponse Create(string fileId, ImmutableList<ContourLevelDto> levels)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new ContoursResponse(fileId, levels);
    }
}
