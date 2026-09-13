using System.Collections.Immutable;

namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record WorldToPixelBatchResponse
{
    private WorldToPixelBatchResponse(string fileId, ImmutableList<WorldPixelPairDto> points)
    {
        FileId = fileId;
        Points = points;
    }

    public string FileId { get; }

    public ImmutableList<WorldPixelPairDto> Points { get; }

    public static WorldToPixelBatchResponse Create(string fileId, ImmutableList<WorldPixelPairDto> points)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new WorldToPixelBatchResponse(fileId, points);
    }
}
