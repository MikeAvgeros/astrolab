using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Download;

public sealed record DownloadRequest(ArchiveSource Archive, string DatasetId);

/// <summary>Static factory accompanying <see cref="DownloadRequest"/>. Validates arguments before constructing.</summary>
public static class DownloadRequestFactory
{
    public static DownloadRequest Create(ArchiveSource archive, string datasetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetId);

        return new DownloadRequest(archive, datasetId);
    }
}
