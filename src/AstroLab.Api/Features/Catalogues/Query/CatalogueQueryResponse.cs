using System.Collections.Immutable;
using AstroLab.Infrastructure.Catalogues;

namespace AstroLab.Api.Features.Catalogues.Query;

public sealed record CatalogueQueryResponse
{
    private CatalogueQueryResponse(string catalogueId, ImmutableList<CatalogueEntryDto> entries)
    {
        CatalogueId = catalogueId;
        Entries = entries;
    }

    public string CatalogueId { get; }

    public ImmutableList<CatalogueEntryDto> Entries { get; }

    public static CatalogueQueryResponse Create(string catalogueId, IReadOnlyList<CatalogueRecord> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogueId);

        return new CatalogueQueryResponse(catalogueId, [.. entries.Select(CatalogueEntryDto.Create)]);
    }
}
