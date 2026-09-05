using AstroLab.Core.Result;
using AstroLab.Infrastructure.Archives;

namespace AstroLab.Tests.Features;

internal sealed class StubEsoArchiveClient : IEsoArchiveClient
{
    private readonly Func<ArchiveSearchQuery, CancellationToken, Task<Result<IReadOnlyList<ArchiveObservation>>>> _search;
    private readonly Func<string, CancellationToken, Task<Result<ArchiveDownload>>> _download;

    public StubEsoArchiveClient(
        Func<ArchiveSearchQuery, CancellationToken, Task<Result<IReadOnlyList<ArchiveObservation>>>> search,
        Func<string, CancellationToken, Task<Result<ArchiveDownload>>> download)
    {
        _search = search;
        _download = download;
    }

    public ArchiveSource Source => ArchiveSource.Eso;

    public Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(ArchiveSearchQuery query, CancellationToken cancellationToken = default) =>
        _search(query, cancellationToken);

    public Task<Result<ArchiveDownload>> DownloadAsync(string datasetId, CancellationToken cancellationToken = default) =>
        _download(datasetId, cancellationToken);

    public Task<Result<IReadOnlyList<EsoProduct>>> GetProductsAsync(string datasetId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"{nameof(StubEsoArchiveClient)} does not support {nameof(GetProductsAsync)}.");

    public Task<Result<ArchiveDownload>> DownloadAsync(EsoProduct product, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"{nameof(StubEsoArchiveClient)} does not support downloading by product.");
}

internal sealed class StubMastArchiveClient : IMastArchiveClient
{
    private readonly Func<ArchiveSearchQuery, CancellationToken, Task<Result<IReadOnlyList<ArchiveObservation>>>> _search;
    private readonly Func<string, CancellationToken, Task<Result<ArchiveDownload>>> _download;

    public StubMastArchiveClient(
        Func<ArchiveSearchQuery, CancellationToken, Task<Result<IReadOnlyList<ArchiveObservation>>>> search,
        Func<string, CancellationToken, Task<Result<ArchiveDownload>>> download)
    {
        _search = search;
        _download = download;
    }

    public ArchiveSource Source => ArchiveSource.Mast;

    public Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(ArchiveSearchQuery query, CancellationToken cancellationToken = default) =>
        _search(query, cancellationToken);

    public Task<Result<ArchiveDownload>> DownloadAsync(string datasetId, CancellationToken cancellationToken = default) =>
        _download(datasetId, cancellationToken);

    public Task<Result<MastTarget>> ResolveTargetAsync(string target, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"{nameof(StubMastArchiveClient)} does not support {nameof(ResolveTargetAsync)}.");

    public Task<Result<IReadOnlyList<MastProduct>>> GetProductsAsync(string observationId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"{nameof(StubMastArchiveClient)} does not support {nameof(GetProductsAsync)}.");

    public Task<Result<ArchiveDownload>> DownloadAsync(MastProduct product, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"{nameof(StubMastArchiveClient)} does not support downloading by product.");
}
