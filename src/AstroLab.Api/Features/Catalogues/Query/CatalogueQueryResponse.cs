using System.Collections.Immutable;

namespace AstroLab.Api.Features.Catalogues.Query;

public sealed record CatalogueQueryResponse
{
    private CatalogueQueryResponse(ImmutableList<CatalogueEntryDto> entries)
    {
        Entries = entries;
    }

    public ImmutableList<CatalogueEntryDto> Entries { get; }

    public static CatalogueQueryResponse Create(ImmutableList<CatalogueEntryDto> entries) =>
        new(entries);
}
