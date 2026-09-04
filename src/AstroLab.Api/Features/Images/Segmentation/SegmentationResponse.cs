using System.Collections.Immutable;

namespace AstroLab.Api.Features.Images.Segmentation;

public sealed record SegmentationResponse
{
    private SegmentationResponse(string fileId, ImmutableList<SegmentDto> segments)
    {
        FileId = fileId;
        Segments = segments;
    }

    public string FileId { get; }

    public ImmutableList<SegmentDto> Segments { get; }

    public static SegmentationResponse Create(string fileId, ImmutableList<SegmentDto> segments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new SegmentationResponse(fileId, segments);
    }
}
