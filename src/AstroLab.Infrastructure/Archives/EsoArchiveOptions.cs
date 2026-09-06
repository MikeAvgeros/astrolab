namespace AstroLab.Infrastructure.Archives;

/// <summary>Configuration bound from the <c>Archives:Eso</c> section, providing the base address for ESO's TAP/DataLink endpoints.</summary>
public sealed class EsoArchiveOptions
{
    public const string SectionName = "Archives:Eso";
    
    public string BaseAddress { get; init; } = "https://archive.eso.org/";
}
