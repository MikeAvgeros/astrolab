using System.Collections.Immutable;

namespace AstroLab.Api.Features.Images.SourceCharacterization;

public sealed record SourceCharacterizationResponse
{
    private SourceCharacterizationResponse(string fileId, ImmutableList<SourceShapeDto> sources)
    {
        FileId = fileId;
        Sources = sources;
    }

    public string FileId { get; }

    public ImmutableList<SourceShapeDto> Sources { get; }

    public static SourceCharacterizationResponse Create(string fileId, ImmutableList<SourceShapeDto> sources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new SourceCharacterizationResponse(fileId, sources);
    }
}
