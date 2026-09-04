namespace AstroLab.Infrastructure.Archives;

public sealed class MastArchiveOptions
{
    public const string SectionName = "Archives:Mast";
    
    public string BaseAddress { get; init; } = "https://mast.stsci.edu/";
}
