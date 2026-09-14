namespace AstroLab.Api.Features.Measurements.StellarColour;

public sealed record StellarColourResponse
{
    private StellarColourResponse(
        string fileId, string comparisonFileId,
        double primaryMagnitude, double primaryMagnitudeUncertainty,
        double secondaryMagnitude, double secondaryMagnitudeUncertainty,
        double colourIndex, double colourIndexUncertainty,
        double zeroPoint)
    {
        FileId = fileId;
        ComparisonFileId = comparisonFileId;
        PrimaryMagnitude = primaryMagnitude;
        PrimaryMagnitudeUncertainty = primaryMagnitudeUncertainty;
        SecondaryMagnitude = secondaryMagnitude;
        SecondaryMagnitudeUncertainty = secondaryMagnitudeUncertainty;
        ColourIndex = colourIndex;
        ColourIndexUncertainty = colourIndexUncertainty;
        ZeroPoint = zeroPoint;
    }

    public string FileId { get; }

    public string ComparisonFileId { get; }

    public double PrimaryMagnitude { get; }

    public double PrimaryMagnitudeUncertainty { get; }

    public double SecondaryMagnitude { get; }

    public double SecondaryMagnitudeUncertainty { get; }

    public double ColourIndex { get; }

    public double ColourIndexUncertainty { get; }
    
    public double ZeroPoint { get; }

    public static StellarColourResponse Create(
        string fileId, string comparisonFileId,
        double primaryMagnitude, double primaryMagnitudeUncertainty,
        double secondaryMagnitude, double secondaryMagnitudeUncertainty,
        double colourIndex, double colourIndexUncertainty,
        double zeroPoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(comparisonFileId);

        return new StellarColourResponse(
            fileId, comparisonFileId,
            primaryMagnitude, primaryMagnitudeUncertainty,
            secondaryMagnitude, secondaryMagnitudeUncertainty,
            colourIndex, colourIndexUncertainty,
            zeroPoint);
    }
}
