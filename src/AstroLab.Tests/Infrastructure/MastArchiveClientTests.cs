using AstroLab.Core.Result;
using AstroLab.Infrastructure.Archives;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroLab.Tests.Infrastructure;

public class MastArchiveClientTests
{
    private static MastArchiveClient CreateClient(
        Func<string, Task<Result<IReadOnlyList<MastProduct>>>> getProducts,
        StubMastArchiveDownloadClient downloadClient) =>
        new(new StubMastArchiveApiClient(getProducts), downloadClient, NullLogger<MastArchiveClient>.Instance);

    [Fact]
    public async Task DownloadAsync_ByObservationId_SelectsBestProductAndDelegatesToDownloadClient()
    {
        var raw = MastProduct.Create("mast:HST/product/x_raw.fits", "x_raw.fits", "SCIENCE", "image", 1, 100, "PUBLIC");
        var calibrated = MastProduct.Create("mast:HST/product/x_drz.fits", "x_drz.fits", "SCIENCE", "image", 3, 200, "PUBLIC");

        var downloadClient = new StubMastArchiveDownloadClient(
            (_, _) => Task.FromResult(Result<ArchiveDownload>.Success(new ArchiveDownload(
                "x_drz.fits", null, System.IO.Pipelines.PipeReader.Create(Stream.Null), new HttpResponseMessage()))));

        var client = CreateClient(
            _ => Task.FromResult(Result<IReadOnlyList<MastProduct>>.Success([raw, calibrated])),
            downloadClient);

        var result = await client.DownloadAsync("obs1");

        Assert.True(result.IsSuccess);
        await using var download = result.Value;

        Assert.Equal(calibrated, Assert.Single(downloadClient.DownloadedProducts));
    }

    [Fact]
    public async Task DownloadAsync_ByObservationId_NoSuitableProduct_ReturnsNotFoundFailure()
    {
        var downloadClient = new StubMastArchiveDownloadClient(
            (_, _) => throw new InvalidOperationException("should not be called"));

        var client = CreateClient(
            _ => Task.FromResult(Result<IReadOnlyList<MastProduct>>.Success([])),
            downloadClient);

        var result = await client.DownloadAsync("obs1");

        Assert.True(result.IsFailure);
        Assert.Equal("mast.no_suitable_product", result.Error.Code);
    }

    [Fact]
    public async Task DownloadAsync_BlankDatasetId_ReturnsValidationFailure()
    {
        var downloadClient = new StubMastArchiveDownloadClient(
            (_, _) => throw new InvalidOperationException("should not be called"));

        var client = CreateClient(
            _ => throw new InvalidOperationException("should not be called"),
            downloadClient);

        var result = await client.DownloadAsync(string.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal("mast.invalid_dataset_id", result.Error.Code);
    }

    [Fact]
    public void MastProductSelectionPolicy_PrefersCalibratedScienceFitsOverRawFits()
    {
        var raw = MastProduct.Create("mast:HST/product/x_raw.fits", "x_raw.fits", "SCIENCE", "image", 1, 100, "PUBLIC");
        var calibrated = MastProduct.Create("mast:HST/product/x_drz.fits", "x_drz.fits", "SCIENCE", "image", 3, 200, "PUBLIC");

        var selected = MastProductSelectionPolicy.SelectBest([raw, calibrated]);

        Assert.Equal(calibrated, selected);
    }

    [Fact]
    public void MastProductSelectionPolicy_IgnoresNonFitsProducts()
    {
        var preview = MastProduct.Create("mast:HST/product/x.jpg", "x.jpg", "PREVIEW", "image", 3, 10, "PUBLIC");
        var fits = MastProduct.Create("mast:HST/product/x_raw.fits", "x_raw.fits", "SCIENCE", "image", 1, 100, "PUBLIC");

        var selected = MastProductSelectionPolicy.SelectBest([preview, fits]);

        Assert.Equal(fits, selected);
    }

    [Fact]
    public void MastProductSelectionPolicy_NoFitsProducts_ReturnsNull()
    {
        var preview = MastProduct.Create("mast:HST/product/x.jpg", "x.jpg", "PREVIEW", "image", 3, 10, "PUBLIC");

        var selected = MastProductSelectionPolicy.SelectBest([preview]);

        Assert.Null(selected);
    }

    private sealed class StubMastArchiveApiClient : IMastArchiveApiClient
    {
        private readonly Func<string, Task<Result<IReadOnlyList<MastProduct>>>> _getProducts;

        public StubMastArchiveApiClient(Func<string, Task<Result<IReadOnlyList<MastProduct>>>> getProducts)
        {
            _getProducts = getProducts;
        }

        public Task<Result<MastTarget>> ResolveTargetAsync(string target, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(
            ArchiveSearchQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<MastProduct>>> GetProductsAsync(
            string observationId, CancellationToken cancellationToken = default) =>
            _getProducts(observationId);
    }

    private sealed class StubMastArchiveDownloadClient : IMastArchiveDownloadClient
    {
        private readonly Func<MastProduct, CancellationToken, Task<Result<ArchiveDownload>>> _download;

        public StubMastArchiveDownloadClient(Func<MastProduct, CancellationToken, Task<Result<ArchiveDownload>>> download)
        {
            _download = download;
        }

        public List<MastProduct> DownloadedProducts { get; } = [];

        public Task<Result<ArchiveDownload>> DownloadAsync(MastProduct product, CancellationToken cancellationToken = default)
        {
            DownloadedProducts.Add(product);
            return _download(product, cancellationToken);
        }
    }
}
