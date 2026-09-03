namespace AstroLab.Infrastructure.Storage;

public readonly record struct StoredFile
{
    private StoredFile(string relativeKey, string fullPath, long sizeBytes)
    {
        RelativeKey = relativeKey;
        FullPath = fullPath;
        SizeBytes = sizeBytes;
    }

    public string RelativeKey { get; }

    public string FullPath { get; }

    public long SizeBytes { get; }

    public static StoredFile Create(string relativeKey, string fullPath, long sizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeKey);

        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);

        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);

        return new StoredFile(relativeKey, fullPath, sizeBytes);
    }
}
