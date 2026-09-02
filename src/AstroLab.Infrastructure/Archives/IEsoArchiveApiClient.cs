using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Archives;

public interface IEsoArchiveApiClient
{
    Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(ArchiveSearchQuery query, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EsoProduct>>> GetProductsAsync(string datasetId, CancellationToken cancellationToken = default);
}
