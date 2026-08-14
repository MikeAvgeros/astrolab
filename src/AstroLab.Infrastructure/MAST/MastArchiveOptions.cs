namespace AstroLab.Infrastructure.MAST;

/// <summary>Configuration for <see cref="MastArchiveClient"/>.</summary>
public sealed class MastArchiveOptions
{
    public const string SectionName = "Archives:Mast";

    /// <summary>Base address of the MAST archive API.</summary>
    public string BaseAddress { get; set; } = "https://mast.stsci.edu/";
}
