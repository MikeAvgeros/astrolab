using System.Collections.Immutable;

namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record PixelToWorldBatchResponse
{
    private PixelToWorldBatchResponse(string fileId, ImmutableList<PixelWorldPairDto> points)
    {
        FileId = fileId;
        Points = points;
    }

    public string FileId { get; }

    public ImmutableList<PixelWorldPairDto> Points { get; }

    public static PixelToWorldBatchResponse Create(string fileId, ImmutableList<PixelWorldPairDto> points)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new PixelToWorldBatchResponse(fileId, points);
    }
}
