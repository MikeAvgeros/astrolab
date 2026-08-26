namespace AstroLab.Infrastructure.Storage;

/// <summary>Describes a file that has been written to local staging storage.</summary>
public readonly record struct StoredFile
{
    private StoredFile(string relativeKey, string fullPath, long sizeBytes)
    {
        RelativeKey = relativeKey;
        FullPath = fullPath;
        SizeBytes = sizeBytes;
    }

    /// <summary>The caller-supplied logical key (e.g. <c>"eso/eso-12345.fits"</c>).</summary>
    public string RelativeKey { get; }

    /// <summary>The absolute path on disk where the file was written.</summary>
    public string FullPath { get; }

    /// <summary>The total number of bytes written.</summary>
    public long SizeBytes { get; }

    public static StoredFile Create(string relativeKey, string fullPath, long sizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeKey);

        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);

        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);

        return new StoredFile(relativeKey, fullPath, sizeBytes);
    }
}
