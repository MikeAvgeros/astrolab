namespace AstroLab.Infrastructure.Catalogues;

/// <summary>A single source returned by an external catalogue cone search, mapped from the catalogue's native wire format.</summary>
public readonly record struct CatalogueRecord
{
    private CatalogueRecord(string identifier, double rightAscension, double declination, double? magnitude)
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

    public static CatalogueRecord Create(string identifier, double rightAscension, double declination, double? magnitude = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        return new CatalogueRecord(identifier, rightAscension, declination, magnitude);
    }
}
