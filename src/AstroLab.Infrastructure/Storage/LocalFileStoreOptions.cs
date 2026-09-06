namespace AstroLab.Infrastructure.Storage;

/// <summary>Configuration bound from the <c>Storage</c> section, controlling where staged FITS files live on disk and the largest upload accepted.</summary>
public sealed class LocalFileStoreOptions
{
    public const string SectionName = "Storage";

    private const long DefaultMaxUploadSizeBytes = 10L * 1024 * 1024 * 1024;

    public string RootPath { get; set; } = "storage";

    public long? MaxUploadSizeBytes { get; set; } = DefaultMaxUploadSizeBytes;
}
