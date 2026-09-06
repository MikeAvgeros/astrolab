using System.Collections.Immutable;
using AstroLab.Core.Fits;
using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// High-level facade combining <see cref="ILocalFileStore"/>, <see cref="FitsHeaderReader"/>,
/// <see cref="FitsPixelDataReader"/>, <see cref="FitsPixelConverter"/>, and
/// <see cref="CfitsIoTimeSeriesReader"/> into the operations API feature slices actually need:
/// inspecting every HDU in a staged file, loading the pixel data of the first HDU that matches a
/// required <see cref="FitsDatasetKind"/>, and loading a time-series table's TIME/FLUX columns
/// (each validated up front via <see cref="FitsDatasetClassifier.EnsureKind"/>, so an analysis
/// never runs against the wrong kind of data). Keeping this orchestration here — rather than
/// duplicated across the Images, Spectroscopy, and TimeSeries feature slices — is what lets those
/// endpoints stay thin.
/// </summary>
public sealed class FitsDatasetReader
{
    private readonly ILocalFileStore _fileStore;

    public FitsDatasetReader(ILocalFileStore fileStore)
    {
        _fileStore = fileStore;
    }

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

    public Task<Result<FitsDataset>> LoadImageAsync(string relativeKey, CancellationToken cancellationToken = default) =>
        LoadPixelDataAsync(relativeKey, FitsDatasetKind.Image, cancellationToken);
    
    public Task<Result<FitsDataset>> LoadSpectrumImageAsync(string relativeKey, CancellationToken cancellationToken = default) =>
        LoadPixelDataAsync(relativeKey, FitsDatasetKind.Spectrum, cancellationToken);

    public async Task<Result<LightCurveTableData>> LoadLightCurveAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        var pathResult = _fileStore.ResolvePath(relativeKey);

        if (pathResult.IsFailure)
        {
            return Result<LightCurveTableData>.Failure(pathResult.Error);
        }

        var openResult = _fileStore.OpenRead(relativeKey);

        if (openResult.IsFailure)
        {
            return Result<LightCurveTableData>.Failure(openResult.Error);
        }

        ImmutableArray<HduLocation> locations;

        await using (var stream = openResult.Value)
        {
            var locationsResult = await FitsHeaderReader.ReadAllHeadersAsync(stream, cancellationToken);

            if (locationsResult.IsFailure)
            {
                return Result<LightCurveTableData>.Failure(locationsResult.Error);
            }

            locations = locationsResult.Value;
        }

        var kindResult = FitsDatasetClassifier.EnsureKind(new HduLocationDescriptorView(locations), FitsDatasetKind.TimeSeries);

        if (kindResult.IsFailure)
        {
            return Result<LightCurveTableData>.Failure(kindResult.Error);
        }

        var tableLocation = FindMatchingLocation(locations, FitsDatasetKind.TimeSeries);

        if (tableLocation is not { } location)
        {
            return Error.Validation("fits.data.no_table", "The file does not contain a time-series table HDU.");
        }

        var descriptorResult = TimeSeriesTableDescriptor.Resolve(location.Descriptor);

        if (descriptorResult.IsFailure)
        {
            return Result<LightCurveTableData>.Failure(descriptorResult.Error);
        }

        return await CfitsIoTimeSeriesReader.ReadAsync(
            pathResult.Value, location.Descriptor.Index + 1, descriptorResult.Value, cancellationToken);
    }

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

        var imageLocation = FindMatchingLocation(locations, requiredKind);

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

        var pixelBuffer = FitsPixelConverter.ToFloatBuffer(buffer.AsSpan(), descriptor);

        return new FitsDataset(location.Descriptor, descriptor, pixelBuffer);
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

    private static HduLocation? FindMatchingLocation(ImmutableArray<HduLocation> locations, FitsDatasetKind requiredKind)
    {
        foreach (var location in locations)
        {
            if (FitsDatasetClassifier.MatchesKind(location.Descriptor, requiredKind))
            {
                return location;
            }
        }

        return null;
    }
}
