using AstroLab.Core.Result;
using AstroLab.Infrastructure.Catalogues;

namespace AstroLab.Tests.Features;

internal sealed class StubCatalogueClient : ICatalogueClient
{
    private readonly Func<CatalogueConeSearchQuery, CancellationToken, Task<Result<IReadOnlyList<CatalogueRecord>>>> _coneSearch;

    public StubCatalogueClient(Func<CatalogueConeSearchQuery, CancellationToken, Task<Result<IReadOnlyList<CatalogueRecord>>>> coneSearch)
    {
        _coneSearch = coneSearch;
    }

    public Task<Result<IReadOnlyList<CatalogueRecord>>> ConeSearchAsync(CatalogueConeSearchQuery query, CancellationToken cancellationToken = default) =>
        _coneSearch(query, cancellationToken);
}
