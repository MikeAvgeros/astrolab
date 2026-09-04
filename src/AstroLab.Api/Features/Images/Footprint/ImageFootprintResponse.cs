using System.Collections.Immutable;

namespace AstroLab.Api.Features.Images.Footprint;

public sealed record ImageFootprintResponse
{
    private ImageFootprintResponse(string fileId, ImmutableList<WorldPointDto> corners)
    {
        FileId = fileId;
        Corners = corners;
    }

    public string FileId { get; }

    public ImmutableList<WorldPointDto> Corners { get; }

    public static ImageFootprintResponse Create(string fileId, ImmutableList<WorldPointDto> corners)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new ImageFootprintResponse(fileId, corners);
    }
}
