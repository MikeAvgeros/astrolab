using AstroLab.Infrastructure.Catalogues;

namespace AstroLab.Api.Features.Catalogues.Query;

public sealed record CatalogueEntryDto
{
    private CatalogueEntryDto(string identifier, double rightAscension, double declination, double? magnitude)
    {
        Identifier = identifier;
        RightAscension = rightAscension;
        Declination = declination;
        Magnitude = magnitude;
    }

    public string Identifier { get; }

    public double RightAscension { get; }

    public double Declination { get; }

    public double? Magnitude { get; }

    public static CatalogueEntryDto Create(CatalogueRecord record)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(record.Identifier);

        return new CatalogueEntryDto(record.Identifier, record.RightAscension, record.Declination, record.Magnitude);
    }
}
