using System.Collections.Immutable;

namespace AstroLab.Api.Features.Images.MultiPhotometry;

public sealed record MultiAperturePhotometryResponse
{
    private MultiAperturePhotometryResponse(string fileId, ImmutableList<SourcePhotometryDto> sources)
    {
        FileId = fileId;
        Sources = sources;
    }

    public string FileId { get; }

    public ImmutableList<SourcePhotometryDto> Sources { get; }

    public static MultiAperturePhotometryResponse Create(string fileId, ImmutableList<SourcePhotometryDto> sources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new MultiAperturePhotometryResponse(fileId, sources);
    }
}
