using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Archives;

public interface IMastArchiveApiClient
{
    Task<Result<MastTarget>> ResolveTargetAsync(string target, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(ArchiveSearchQuery query, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<MastProduct>>> GetProductsAsync(string observationId, CancellationToken cancellationToken = default);
}
