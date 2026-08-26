using System.Collections.Immutable;

namespace AstroLab.Api.Features.Images.Sources;

public sealed record SourceDetectionResponse
{
    private SourceDetectionResponse(string fileId, ImmutableList<DetectedSourceDto> sources)
    {
        FileId = fileId;
        Sources = sources;
    }

    public string FileId { get; }

    public ImmutableList<DetectedSourceDto> Sources { get; }

    public static SourceDetectionResponse Create(string fileId, ImmutableList<DetectedSourceDto> sources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new SourceDetectionResponse(fileId, sources);
    }
}
