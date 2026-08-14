using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Archives;

/// <summary>
/// Common contract implemented by every upstream archive integration (ESO, MAST, ...). Keeping
/// this abstraction stable lets each archive's HTTP implementation evolve independently without
/// any change rippling into <c>AstroLab.Core</c> or the API feature slices that consume it.
/// </summary>
public interface IArchiveClient
{
    ArchiveSource Source { get; }

    Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(ArchiveSearchQuery query, CancellationToken cancellationToken = default);

    Task<Result<ArchiveDownload>> DownloadAsync(string datasetId, CancellationToken cancellationToken = default);
}
