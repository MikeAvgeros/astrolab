namespace AstroLab.Infrastructure.Catalogues;

/// <summary>Configuration bound from the <c>Catalogues:Vizier</c> section, providing the base address for VizieR's IVOA TAP service.</summary>
public sealed class VizierOptions
{
    public const string SectionName = "Catalogues:Vizier";

    public string BaseAddress { get; init; } = "https://tapvizier.cds.unistra.fr/TAPVizieR/tap/";
}
