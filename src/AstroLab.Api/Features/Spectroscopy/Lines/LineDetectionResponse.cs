using System.Collections.Immutable;

namespace AstroLab.Api.Features.Spectroscopy.Lines;

public sealed record LineDetectionResponse
{
    private LineDetectionResponse(string fileId, ImmutableList<SpectralLineDto> lines)
    {
        FileId = fileId;
        Lines = lines;
    }

    public string FileId { get; }

    public ImmutableList<SpectralLineDto> Lines { get; }

    public static LineDetectionResponse Create(string fileId, ImmutableList<SpectralLineDto> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new LineDetectionResponse(fileId, lines);
    }
}
