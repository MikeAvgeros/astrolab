namespace AstroLab.Api.Features.Catalogues.CrossMatch;

public sealed record CrossMatchEntryDto
{
    private CrossMatchEntryDto(string catalogueIdentifier, double separationArcsec)
    {
        CatalogueIdentifier = catalogueIdentifier;
        SeparationArcsec = separationArcsec;
    }

    public string CatalogueIdentifier { get; }

    public double SeparationArcsec { get; }

    public static CrossMatchEntryDto Create(string catalogueIdentifier, double separationArcsec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogueIdentifier);

        return new CrossMatchEntryDto(catalogueIdentifier, separationArcsec);
    }
}
