namespace AstroLab.Infrastructure.Archives;

public sealed class EsoArchiveOptions
{
    public const string SectionName = "Archives:Eso";
    
    public string BaseAddress { get; set; } = "https://archive.eso.org/";
}
