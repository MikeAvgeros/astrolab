namespace AstroLab.Infrastructure.Archives;

public sealed class MastArchiveOptions
{
    public const string SectionName = "Archives:Mast";
    
    public string BaseAddress { get; set; } = "https://mast.stsci.edu/";
}
