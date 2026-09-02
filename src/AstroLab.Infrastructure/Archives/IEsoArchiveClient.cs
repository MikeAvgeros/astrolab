using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Archives;

public interface IEsoArchiveClient : IArchiveClient
{
    Task<Result<IReadOnlyList<EsoProduct>>> GetProductsAsync(string datasetId, CancellationToken cancellationToken = default);

    Task<Result<ArchiveDownload>> DownloadAsync(EsoProduct product, CancellationToken cancellationToken = default);
}
