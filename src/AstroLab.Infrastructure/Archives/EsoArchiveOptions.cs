namespace AstroLab.Infrastructure.Archives;

public sealed class EsoArchiveOptions
{
    public const string SectionName = "Archives:Eso";
    
    public string BaseAddress { get; init; } = "https://archive.eso.org/";
}
