using System.IO.Pipelines;
using AstroLab.Core.Result;
using Microsoft.Extensions.Options;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// Local-disk implementation of <see cref="ILocalFileStore"/>. Streams incoming data to disk via
/// <see cref="PipeReader"/> so that even multi-gigabyte FITS files never need to be materialized
/// as a single managed <c>byte[]</c>.
/// </summary>
public sealed class LocalFileStore : ILocalFileStore
{
    private const int DefaultFileStreamBufferSize = 4096;

    private readonly string _rootPath;

    public LocalFileStore(IOptions<LocalFileStoreOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public Result<string> ResolvePath(string relativeKey)
    {
        if (string.IsNullOrWhiteSpace(relativeKey))
        {
            return Error.Validation("storage.invalid_key", "relativeKey must not be empty.");
        }

        var combined = Path.GetFullPath(Path.Combine(_rootPath, relativeKey));
        var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;

        if (combined != _rootPath && !combined.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            return Error.Validation("storage.path_traversal_rejected", $"'{relativeKey}' resolves outside the storage root.");
        }

        return combined;
    }

    public string CreateStagingKey(string? fileExtension = null)
    {
        var name = Guid.NewGuid().ToString("N");
        return string.IsNullOrEmpty(fileExtension) ? name : $"{name}.{fileExtension.TrimStart('.')}";
    }

    public async Task<Result<StoredFile>> WriteAsync(string relativeKey, PipeReader source, CancellationToken cancellationToken = default)
    {
        var pathResult = ResolvePath(relativeKey);
        if (pathResult.IsFailure)
        {
            await source.CompleteAsync();
            return Result<StoredFile>.Failure(pathResult.Error);
        }

        var path = pathResult.Value;
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var succeeded = false;
        long totalBytesWritten = 0;
        FileStream? fileStream = null;
        try
        {
            fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: DefaultFileStreamBufferSize, useAsync: true);

            while (true)
            {
                var readResult = await source.ReadAsync(cancellationToken);
                var buffer = readResult.Buffer;

                foreach (var segment in buffer)
                {
                    await fileStream.WriteAsync(segment, cancellationToken);
                    totalBytesWritten += segment.Length;
                }

                source.AdvanceTo(buffer.End);

                if (readResult.IsCompleted)
                {
                    break;
                }
            }

            await fileStream.FlushAsync(cancellationToken);
            await source.CompleteAsync();
            succeeded = true;
            return new StoredFile(relativeKey, path, totalBytesWritten);
        }
        catch (OperationCanceledException ex)
        {
            await source.CompleteAsync(ex);
            throw;
        }
        catch (IOException ex)
        {
            await source.CompleteAsync(ex);
            return Error.Infrastructure("storage.write_failed", $"Failed to write staged file '{relativeKey}': {ex.Message}");
        }
        finally
        {
            if (fileStream is not null)
            {
                await fileStream.DisposeAsync();
            }

            if (!succeeded)
            {
                TryDeleteFile(path);
            }
        }
    }

    public Result<Stream> OpenRead(string relativeKey)
    {
        var pathResult = ResolvePath(relativeKey);
        if (pathResult.IsFailure)
        {
            return Result<Stream>.Failure(pathResult.Error);
        }

        var path = pathResult.Value;
        if (!File.Exists(path))
        {
            return Error.NotFound("storage.file_not_found", $"No staged file exists at '{relativeKey}'.");
        }

        try
        {
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: DefaultFileStreamBufferSize, useAsync: true);
            return Result<Stream>.Success(stream);
        }
        catch (IOException ex)
        {
            return Error.Infrastructure("storage.open_failed", $"Failed to open staged file '{relativeKey}': {ex.Message}");
        }
    }

    public bool Exists(string relativeKey) =>
        ResolvePath(relativeKey) is { IsSuccess: true } result && File.Exists(result.Value);

    public Result<Unit> Delete(string relativeKey)
    {
        var pathResult = ResolvePath(relativeKey);
        if (pathResult.IsFailure)
        {
            return Result<Unit>.Failure(pathResult.Error);
        }

        try
        {
            File.Delete(pathResult.Value);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (IOException ex)
        {
            return Error.Infrastructure("storage.delete_failed", $"Failed to delete staged file '{relativeKey}': {ex.Message}");
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }
}
