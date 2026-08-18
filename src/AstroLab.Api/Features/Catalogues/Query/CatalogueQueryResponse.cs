namespace AstroLab.Api.Features.Catalogues.Query;

public sealed record CatalogueQueryResponse(IReadOnlyList<CatalogueEntryDto> Entries);
