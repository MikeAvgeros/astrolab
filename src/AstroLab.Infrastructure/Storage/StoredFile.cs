namespace AstroLab.Infrastructure.Storage;

/// <summary>Describes a file that has been written to local staging storage.</summary>
/// <param name="RelativeKey">The caller-supplied logical key (e.g. <c>"eso/eso-12345.fits"</c>).</param>
/// <param name="FullPath">The absolute path on disk where the file was written.</param>
/// <param name="SizeBytes">The total number of bytes written.</param>
public readonly record struct StoredFile(string RelativeKey, string FullPath, long SizeBytes);
