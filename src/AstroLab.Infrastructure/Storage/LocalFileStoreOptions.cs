namespace AstroLab.Infrastructure.Storage;

public sealed class LocalFileStoreOptions
{
    public const string SectionName = "Storage";

    private const long DefaultMaxUploadSizeBytes = 10L * 1024 * 1024 * 1024;

    public string RootPath { get; set; } = "storage";

    public long? MaxUploadSizeBytes { get; set; } = DefaultMaxUploadSizeBytes;
}
