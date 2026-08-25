using System.Collections.Immutable;

namespace AstroLab.Api.Features.Images.Sources;

public sealed record SourceDetectionResponse(string FileId, ImmutableList<DetectedSourceDto> Sources);

/// <summary>Static factory accompanying <see cref="SourceDetectionResponse"/>. Validates arguments before constructing.</summary>
public static class SourceDetectionResponseFactory
{
    public static SourceDetectionResponse Create(string fileId, ImmutableList<DetectedSourceDto> sources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new SourceDetectionResponse(fileId, sources);
    }
}
