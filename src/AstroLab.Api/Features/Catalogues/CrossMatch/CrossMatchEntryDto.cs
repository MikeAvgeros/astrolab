using AstroLab.Core.Catalogues;

namespace AstroLab.Api.Features.Catalogues.CrossMatch;

public sealed record CrossMatchEntryDto
{
    private CrossMatchEntryDto(
        int detectedSourceId, string catalogueId, string catalogueIdentifier,
        double rightAscension, double declination, double? magnitude, double separationArcsec)
    {
        DetectedSourceId = detectedSourceId;
        CatalogueId = catalogueId;
        CatalogueIdentifier = catalogueIdentifier;
        RightAscension = rightAscension;
        Declination = declination;
        Magnitude = magnitude;
        SeparationArcsec = separationArcsec;
    }

    public int DetectedSourceId { get; }

    public string CatalogueId { get; }

    public string CatalogueIdentifier { get; }

    public double RightAscension { get; }

    public double Declination { get; }

    public double? Magnitude { get; }

    public double SeparationArcsec { get; }

    public static CrossMatchEntryDto Create(CatalogueMatch match)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(match.SourceId);

        return new CrossMatchEntryDto(
            match.SourceId,
            match.Candidate.CatalogueId,
            match.Candidate.Identifier,
            match.Candidate.RightAscension,
            match.Candidate.Declination,
            match.Candidate.Magnitude,
            match.SeparationArcsec);
    }
}
