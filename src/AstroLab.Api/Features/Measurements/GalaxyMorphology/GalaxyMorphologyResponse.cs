namespace AstroLab.Api.Features.Measurements.GalaxyMorphology;

public sealed record GalaxyMorphologyResponse
{
    private GalaxyMorphologyResponse(
        string fileId, double? effectiveRadiusPixels, double ellipticity, string estimatedMorphologicalType,
        double? concentrationIndex, string method)
    {
        FileId = fileId;
        EffectiveRadiusPixels = effectiveRadiusPixels;
        Ellipticity = ellipticity;
        EstimatedMorphologicalType = estimatedMorphologicalType;
        ConcentrationIndex = concentrationIndex;
        Method = method;
    }

    public string FileId { get; }

    public double? EffectiveRadiusPixels { get; }

    public double Ellipticity { get; }

    public string EstimatedMorphologicalType { get; }
    
    public double? ConcentrationIndex { get; }

    public string Method { get; }

    public static GalaxyMorphologyResponse Create(
        string fileId, double? effectiveRadiusPixels, double ellipticity, string estimatedMorphologicalType,
        double? concentrationIndex, string method)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        ArgumentException.ThrowIfNullOrWhiteSpace(estimatedMorphologicalType);

        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        return new GalaxyMorphologyResponse(fileId, effectiveRadiusPixels, ellipticity, estimatedMorphologicalType, concentrationIndex, method);
    }
}
