using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Catalogues;

public interface ICatalogueClient
{
    Task<Result<IReadOnlyList<CatalogueRecord>>> ConeSearchAsync(CatalogueConeSearchQuery query, CancellationToken cancellationToken = default);
}
