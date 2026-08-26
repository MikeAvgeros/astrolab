using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Download;

public sealed record DownloadResponse
{
    private DownloadResponse(string fileId, ArchiveSource archive, long sizeBytes)
    {
        FileId = fileId;
        Archive = archive;
        SizeBytes = sizeBytes;
    }

    public string FileId { get; }

    public ArchiveSource Archive { get; }

    public long SizeBytes { get; }

    public static DownloadResponse Create(string fileId, ArchiveSource archive, long sizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);

        return new DownloadResponse(fileId, archive, sizeBytes);
    }
}
