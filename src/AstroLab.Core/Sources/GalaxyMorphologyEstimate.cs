namespace AstroLab.Core.Sources;

public readonly record struct GalaxyMorphologyEstimate
{
    private GalaxyMorphologyEstimate(double effectiveRadiusPixels, double ellipticity, string morphologicalType)
    {
        EffectiveRadiusPixels = effectiveRadiusPixels;
        Ellipticity = ellipticity;
        MorphologicalType = morphologicalType;
    }

    public double EffectiveRadiusPixels { get; }

    public double Ellipticity { get; }

    public string MorphologicalType { get; }

    public static GalaxyMorphologyEstimate Create(double effectiveRadiusPixels, double ellipticity, string morphologicalType)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(effectiveRadiusPixels);

        ArgumentException.ThrowIfNullOrWhiteSpace(morphologicalType);

        return new GalaxyMorphologyEstimate(effectiveRadiusPixels, ellipticity, morphologicalType);
    }
}
