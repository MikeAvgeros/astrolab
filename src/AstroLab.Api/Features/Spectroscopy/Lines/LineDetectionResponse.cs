using System.Collections.Immutable;

namespace AstroLab.Api.Features.Spectroscopy.Lines;

public sealed record LineDetectionResponse(string FileId, ImmutableList<SpectralLineDto> Lines);

/// <summary>Static factory accompanying <see cref="LineDetectionResponse"/>. Validates arguments before constructing.</summary>
public static class LineDetectionResponseFactory
{
    public static LineDetectionResponse Create(string fileId, ImmutableList<SpectralLineDto> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new LineDetectionResponse(fileId, lines);
    }
}
