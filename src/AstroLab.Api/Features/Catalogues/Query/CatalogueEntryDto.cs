namespace AstroLab.Api.Features.Catalogues.Query;

public sealed record CatalogueEntryDto(string Identifier, double RightAscension, double Declination, double Magnitude);

/// <summary>Static factory accompanying <see cref="CatalogueEntryDto"/>. Validates arguments before constructing.</summary>
public static class CatalogueEntryDtoFactory
{
    public static CatalogueEntryDto Create(string identifier, double rightAscension, double declination, double magnitude)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        return new CatalogueEntryDto(identifier, rightAscension, declination, magnitude);
    }
}
