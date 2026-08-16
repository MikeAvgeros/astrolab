using AstroLab.Core.Fits;
using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// High-level facade combining <see cref="ILocalFileStore"/>, <see cref="FitsHeaderReader"/>,
/// <see cref="FitsPixelDataReader"/>, and <see cref="FitsPixelConverter"/> into the single
/// operation API feature slices actually need: "load the primary image of a staged FITS file."
/// Keeping this orchestration here — rather than duplicated across the Imaging, Photometry, and
/// Spectroscopy feature slices — is what lets those endpoints stay thin.
/// </summary>
public sealed class FitsDatasetReader
{
    private readonly ILocalFileStore _fileStore;

    public FitsDatasetReader(ILocalFileStore fileStore)
    {
        _fileStore = fileStore;
    }

    public async Task<Result<FitsHeader>> ReadHeaderAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        var openResult = _fileStore.OpenRead(relativeKey);
        if (openResult.IsFailure)
        {
            return Result<FitsHeader>.Failure(openResult.Error);
        }

        await using var stream = openResult.Value;
        return await FitsHeaderReader.ReadPrimaryHeaderAsync(stream, cancellationToken);
    }

    public async Task<Result<FitsDataset>> LoadPrimaryImageAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        var openResult = _fileStore.OpenRead(relativeKey);
        if (openResult.IsFailure)
        {
            return Result<FitsDataset>.Failure(openResult.Error);
        }

        await using var stream = openResult.Value;

        var headerResult = await FitsHeaderReader.ReadPrimaryHeaderAsync(stream, cancellationToken);
        if (headerResult.IsFailure)
        {
            return Result<FitsDataset>.Failure(headerResult.Error);
        }

        var hdu = HduDescriptor.FromHeader(0, headerResult.Value);
        if (hdu.Image is not { } descriptor || descriptor.PixelCount == 0)
        {
            return Error.Validation("fits.data.no_image", "The primary HDU does not contain image pixel data.");
        }

        var bufferResult = await FitsPixelDataReader.ReadImageDataAsync(stream, descriptor, cancellationToken);
        if (bufferResult.IsFailure)
        {
            return Result<FitsDataset>.Failure(bufferResult.Error);
        }

        using var buffer = bufferResult.Value;
        var pixels = FitsPixelConverter.ToFloatArray(buffer.AsSpan(), descriptor);
        return new FitsDataset(hdu, descriptor, pixels);
    }
}
