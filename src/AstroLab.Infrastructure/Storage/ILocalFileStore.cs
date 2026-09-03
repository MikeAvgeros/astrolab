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
    Result<string> ResolvePath(string relativeKey);

    string CreateStagingKey(string? fileExtension = null);

    Task<Result<StoredFile>> WriteAsync(string relativeKey, PipeReader source, CancellationToken cancellationToken = default);

    Result<Stream> OpenRead(string relativeKey);

    bool Exists(string relativeKey);

    Result<Unit> Delete(string relativeKey);
}
