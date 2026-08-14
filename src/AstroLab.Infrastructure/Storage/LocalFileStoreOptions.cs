namespace AstroLab.Infrastructure.Storage;

/// <summary>Configuration for <see cref="LocalFileStore"/>.</summary>
public sealed class LocalFileStoreOptions
{
    public const string SectionName = "Storage";

    /// <summary>The root directory under which all staged FITS files and archive downloads are kept.</summary>
    public string RootPath { get; set; } = "storage";
}
