namespace AstroLab.Infrastructure.ESO;

/// <summary>Configuration for <see cref="EsoArchiveClient"/>.</summary>
public sealed class EsoArchiveOptions
{
    public const string SectionName = "Archives:Eso";

    /// <summary>Base address of the ESO Science Archive Facility API.</summary>
    public string BaseAddress { get; set; } = "https://archive.eso.org/";
}
