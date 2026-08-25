using System.Collections.Immutable;

namespace AstroLab.Api.Features.Catalogues.Query;

public sealed record CatalogueQueryResponse(ImmutableList<CatalogueEntryDto> Entries);

/// <summary>Static factory accompanying <see cref="CatalogueQueryResponse"/>.</summary>
public static class CatalogueQueryResponseFactory
{
    public static CatalogueQueryResponse Create(ImmutableList<CatalogueEntryDto> entries) =>
        new(entries);
}
