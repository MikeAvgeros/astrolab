namespace AstroLab.Infrastructure.Storage;

/// <summary>Configuration for <see cref="LocalFileStore"/>.</summary>
public sealed class LocalFileStoreOptions
{
    public const string SectionName = "Storage";

    private const long DefaultMaxUploadSizeBytes = 10L * 1024 * 1024 * 1024;

    /// <summary>The root directory under which all staged FITS files and archive downloads are kept.</summary>
    public string RootPath { get; set; } = "storage";

    /// <summary>
    /// The largest request body the FITS upload endpoint will accept, in bytes. Overrides
    /// Kestrel's default ~28.6 MB request body cap, which would otherwise reject any real
    /// astronomical FITS file despite the endpoint streaming the body to disk without buffering
    /// it in memory. Set to <see langword="null"/> to accept a request body of any size.
    /// </summary>
    public long? MaxUploadSizeBytes { get; set; } = DefaultMaxUploadSizeBytes;
}
