namespace AstroLab.Api.Features.Measurements.StellarColour;

public sealed record StellarColourResponse
{
    private StellarColourResponse(string fileId, string comparisonFileId, double primaryMagnitude, double secondaryMagnitude, double colourIndex)
    {
        FileId = fileId;
        ComparisonFileId = comparisonFileId;
        PrimaryMagnitude = primaryMagnitude;
        SecondaryMagnitude = secondaryMagnitude;
        ColourIndex = colourIndex;
    }

    public string FileId { get; }

    public string ComparisonFileId { get; }

    public double PrimaryMagnitude { get; }

    public double SecondaryMagnitude { get; }

    public double ColourIndex { get; }

    public static StellarColourResponse Create(string fileId, string comparisonFileId, double primaryMagnitude, double secondaryMagnitude, double colourIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(comparisonFileId);

        return new StellarColourResponse(fileId, comparisonFileId, primaryMagnitude, secondaryMagnitude, colourIndex);
    }
}
