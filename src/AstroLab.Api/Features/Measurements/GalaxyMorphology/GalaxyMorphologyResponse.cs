namespace AstroLab.Api.Features.Measurements.GalaxyMorphology;

public sealed record GalaxyMorphologyResponse
{
    private GalaxyMorphologyResponse(string fileId, double effectiveRadiusPixels, double ellipticity, string estimatedMorphologicalType)
    {
        FileId = fileId;
        EffectiveRadiusPixels = effectiveRadiusPixels;
        Ellipticity = ellipticity;
        EstimatedMorphologicalType = estimatedMorphologicalType;
    }

    public string FileId { get; }

    public double EffectiveRadiusPixels { get; }

    public double Ellipticity { get; }

    public string EstimatedMorphologicalType { get; }

    public static GalaxyMorphologyResponse Create(string fileId, double effectiveRadiusPixels, double ellipticity, string estimatedMorphologicalType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(estimatedMorphologicalType);

        return new GalaxyMorphologyResponse(fileId, effectiveRadiusPixels, ellipticity, estimatedMorphologicalType);
    }
}
