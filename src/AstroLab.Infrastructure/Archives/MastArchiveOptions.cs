namespace AstroLab.Infrastructure.Archives;

/// <summary>Configuration bound from the <c>Archives:Mast</c> section, providing the base address for MAST's Mashup API endpoints.</summary>
public sealed class MastArchiveOptions
{
    public const string SectionName = "Archives:Mast";
    
    public string BaseAddress { get; init; } = "https://mast.stsci.edu/";
}
