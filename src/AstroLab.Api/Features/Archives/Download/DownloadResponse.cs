using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Download;

public sealed record DownloadResponse(string FileId, ArchiveSource Archive, long SizeBytes);

/// <summary>Static factory accompanying <see cref="DownloadResponse"/>. Validates arguments before constructing.</summary>
public static class DownloadResponseFactory
{
    public static DownloadResponse Create(string fileId, ArchiveSource archive, long sizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);

        return new DownloadResponse(fileId, archive, sizeBytes);
    }
}
