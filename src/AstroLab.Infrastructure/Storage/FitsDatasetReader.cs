using System.Collections.Immutable;
using AstroLab.Core.Fits;
using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// High-level facade combining <see cref="ILocalFileStore"/>, <see cref="FitsHeaderReader"/>,
/// <see cref="FitsPixelDataReader"/>, and <see cref="FitsPixelConverter"/> into the operations API
/// feature slices actually need: inspecting every HDU in a staged file, and loading the pixel data
/// of the first HDU that matches a required <see cref="FitsDatasetKind"/> (validated up front via
/// <see cref="FitsDatasetClassifier.EnsureKind"/>, so an analysis never runs against the wrong kind
/// of data). Keeping this orchestration here — rather than duplicated across the Images and
/// Spectroscopy feature slices — is what lets those endpoints stay thin.
/// </summary>
public sealed class FitsDatasetReader
{
    private readonly ILocalFileStore _fileStore;

    public FitsDatasetReader(ILocalFileStore fileStore)
    {
        _fileStore = fileStore;
    }

    /// <summary>Walks every HDU in a staged file, returning their parsed descriptors in file order.</summary>
    public async Task<Result<ImmutableArray<HduDescriptor>>> ReadAllHdusAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        var openResult = _fileStore.OpenRead(relativeKey);
        if (openResult.IsFailure)
        {
            return Result<ImmutableArray<HduDescriptor>>.Failure(openResult.Error);
        }

        await using var stream = openResult.Value;
        var locationsResult = await FitsHeaderReader.ReadAllHeadersAsync(stream, cancellationToken);
        return locationsResult.Map(ToDescriptors);
    }

    /// <summary>Loads the first image-bearing HDU's pixel data, requiring the file to classify as <see cref="FitsDatasetKind.Image"/>.</summary>
    public Task<Result<FitsDataset>> LoadImageAsync(string relativeKey, CancellationToken cancellationToken = default) =>
        LoadPixelDataAsync(relativeKey, FitsDatasetKind.Image, cancellationToken);

    /// <summary>Loads the first image-bearing HDU's pixel data, requiring the file to classify as <see cref="FitsDatasetKind.Spectrum"/>.</summary>
    public Task<Result<FitsDataset>> LoadSpectrumImageAsync(string relativeKey, CancellationToken cancellationToken = default) =>
        LoadPixelDataAsync(relativeKey, FitsDatasetKind.Spectrum, cancellationToken);

    private async Task<Result<FitsDataset>> LoadPixelDataAsync(string relativeKey, FitsDatasetKind requiredKind, CancellationToken cancellationToken)
    {
        var openResult = _fileStore.OpenRead(relativeKey);
        if (openResult.IsFailure)
        {
            return Result<FitsDataset>.Failure(openResult.Error);
        }

        await using var stream = openResult.Value;

        var locationsResult = await FitsHeaderReader.ReadAllHeadersAsync(stream, cancellationToken);
        if (locationsResult.IsFailure)
        {
            return Result<FitsDataset>.Failure(locationsResult.Error);
        }

        var locations = locationsResult.Value;
        var kindResult = FitsDatasetClassifier.EnsureKind(new HduLocationDescriptorView(locations), requiredKind);
        if (kindResult.IsFailure)
        {
            return Result<FitsDataset>.Failure(kindResult.Error);
        }

        var imageLocation = FindFirstImageLocation(locations);
        if (imageLocation is not { } location || location.Descriptor.Image is not { } descriptor)
        {
            return Error.Validation("fits.data.no_image", "The file does not contain an HDU with pixel data.");
        }

        stream.Seek(location.DataOffset, SeekOrigin.Begin);

        var bufferResult = await FitsPixelDataReader.ReadImageDataAsync(stream, descriptor, cancellationToken);
        if (bufferResult.IsFailure)
        {
            return Result<FitsDataset>.Failure(bufferResult.Error);
        }

        using var buffer = bufferResult.Value;
        var pixels = FitsPixelConverter.ToFloatArray(buffer.AsSpan(), descriptor);
        return new FitsDataset(location.Descriptor, descriptor, pixels);
    }

    private static ImmutableArray<HduDescriptor> ToDescriptors(ImmutableArray<HduLocation> locations)
    {
        var builder = ImmutableArray.CreateBuilder<HduDescriptor>(locations.Length);
        foreach (var location in locations)
        {
            builder.Add(location.Descriptor);
        }

        return builder.MoveToImmutable();
    }

    private static HduLocation? FindFirstImageLocation(ImmutableArray<HduLocation> locations)
    {
        foreach (var location in locations)
        {
            if (FitsDatasetClassifier.HasPixelData(location.Descriptor))
            {
                return location;
            }
        }

        return null;
    }
}
