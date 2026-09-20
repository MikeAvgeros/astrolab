namespace AstroLab.Core.Catalogues;

/// <summary>A single external-catalogue source available as a cross-match candidate.</summary>
public readonly record struct CatalogueMatchCandidate
{
    private CatalogueMatchCandidate(string catalogueId, string identifier, double rightAscension, double declination, double? magnitude)
    {
        CatalogueId = catalogueId;
        Identifier = identifier;
        RightAscension = rightAscension;
        Declination = declination;
        Magnitude = magnitude;
    }

    public string CatalogueId { get; }

    public string Identifier { get; }

    public double RightAscension { get; }

    public double Declination { get; }

    public double? Magnitude { get; }

    public static CatalogueMatchCandidate Create(
        string catalogueId, string identifier, double rightAscension, double declination, double? magnitude = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogueId);

        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        if (!double.IsFinite(rightAscension))
        {
            throw new ArgumentOutOfRangeException(nameof(rightAscension), rightAscension, "rightAscension must be finite.");
        }

        if (!double.IsFinite(declination))
        {
            throw new ArgumentOutOfRangeException(nameof(declination), declination, "declination must be finite.");
        }

        return new CatalogueMatchCandidate(catalogueId, identifier, rightAscension, declination, magnitude);
    }
}
