namespace AstroLab.Api.Features.Catalogues.CrossMatch;

public sealed record CrossMatchEntryDto(string CatalogueIdentifier, double SeparationArcsec);

/// <summary>Static factory accompanying <see cref="CrossMatchEntryDto"/>. Validates arguments before constructing.</summary>
public static class CrossMatchEntryDtoFactory
{
    public static CrossMatchEntryDto Create(string catalogueIdentifier, double separationArcsec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogueIdentifier);

        return new CrossMatchEntryDto(catalogueIdentifier, separationArcsec);
    }
}
