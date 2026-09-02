using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Archives;

public interface IMastArchiveClient : IArchiveClient
{
    Task<Result<MastTarget>> ResolveTargetAsync(string target, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<MastProduct>>> GetProductsAsync(string observationId, CancellationToken cancellationToken = default);

    Task<Result<ArchiveDownload>> DownloadAsync(MastProduct product, CancellationToken cancellationToken = default);
}
