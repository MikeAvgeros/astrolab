using System.IO.Pipelines;
using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// Manages FITS files staged on local disk: archive downloads, user uploads, and rendered
/// derivatives. All paths are resolved relative to a configured root and validated to prevent
/// path traversal outside of it.
/// </summary>
public interface ILocalFileStore
{
    /// <summary>Resolves <paramref name="relativeKey"/> to an absolute path under the store's root, rejecting traversal outside it.</summary>
    Result<string> ResolvePath(string relativeKey);

    /// <summary>Generates a fresh, collision-free relative key for a new staged file, optionally preserving <paramref name="fileExtension"/>.</summary>
    string CreateStagingKey(string? fileExtension = null);

    /// <summary>
    /// Streams <paramref name="source"/> to disk at <paramref name="relativeKey"/> incrementally,
    /// never buffering the whole payload in memory.
    /// </summary>
    Task<Result<StoredFile>> WriteAsync(string relativeKey, PipeReader source, CancellationToken cancellationToken = default);

    /// <summary>Opens a previously staged file for reading.</summary>
    Result<Stream> OpenRead(string relativeKey);

    /// <summary>Returns whether a file exists at <paramref name="relativeKey"/>.</summary>
    bool Exists(string relativeKey);

    /// <summary>Deletes a staged file, if present.</summary>
    Result<Unit> Delete(string relativeKey);
}
