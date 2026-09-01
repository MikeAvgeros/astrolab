using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Archives;

public interface IArchiveClient
{
    ArchiveSource Source { get; }

    Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(ArchiveSearchQuery query, CancellationToken cancellationToken = default);

    Task<Result<ArchiveDownload>> DownloadAsync(string datasetId, CancellationToken cancellationToken = default);
}
