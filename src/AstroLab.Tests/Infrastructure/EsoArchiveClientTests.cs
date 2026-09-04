using AstroLab.Core.Result;
using AstroLab.Infrastructure.Archives;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroLab.Tests.Infrastructure;

public class EsoArchiveClientTests
{
    private static EsoArchiveClient CreateClient(
        Func<string, Task<Result<IReadOnlyList<EsoProduct>>>> getProducts,
        StubEsoArchiveDownloadClient downloadClient) =>
        new(new StubEsoArchiveApiClient(getProducts), downloadClient, NullLogger<EsoArchiveClient>.Instance);

    [Fact]
    public async Task DownloadAsync_ByDatasetId_SelectsBestProductAndDelegatesToDownloadClient()
    {
        var preview = EsoProduct.Create("ADP.123-preview", "ADP.123", null, "https://x/preview", "#preview", null, null, "image/jpeg", 5000, "PUBLIC");
        var primary = EsoProduct.Create("ADP.123-this", "ADP.123", null, "https://x/file", "#this", null, 2, "application/x-fits", 200000, "PUBLIC");

        var downloadClient = new StubEsoArchiveDownloadClient(
            (product, _) => Task.FromResult(Result<ArchiveDownload>.Success(new ArchiveDownload(
                "dataset.fits", null, System.IO.Pipelines.PipeReader.Create(Stream.Null), new HttpResponseMessage()))));

        var client = CreateClient(
            _ => Task.FromResult(Result<IReadOnlyList<EsoProduct>>.Success([preview, primary])),
            downloadClient);

        var result = await client.DownloadAsync("ADP.123");

        Assert.True(result.IsSuccess);
        await using var download = result.Value;

        Assert.Equal(primary, Assert.Single(downloadClient.DownloadedProducts));
    }

    [Fact]
    public async Task DownloadAsync_ByDatasetId_NoProducts_ReturnsNotFoundFailure()
    {
        var downloadClient = new StubEsoArchiveDownloadClient(
            (_, _) => throw new InvalidOperationException("should not be called"));

        var client = CreateClient(
            _ => Task.FromResult(Result<IReadOnlyList<EsoProduct>>.Success([])),
            downloadClient);

        var result = await client.DownloadAsync("ADP.123");

        Assert.True(result.IsFailure);
        Assert.Equal("eso.no_suitable_product", result.Error.Code);
    }

    [Fact]
    public async Task DownloadAsync_BlankDatasetId_ReturnsValidationFailure()
    {
        var downloadClient = new StubEsoArchiveDownloadClient(
            (_, _) => throw new InvalidOperationException("should not be called"));

        var client = CreateClient(
            _ => throw new InvalidOperationException("should not be called"),
            downloadClient);

        var result = await client.DownloadAsync("   ");

        Assert.True(result.IsFailure);
        Assert.Equal("eso.invalid_dataset_id", result.Error.Code);
    }

    [Fact]
    public void EsoProductSelectionPolicy_PrefersPrimaryFitsOverPreview()
    {
        var preview = EsoProduct.Create("p1", "ADP.123", null, "https://x/preview", "#preview", null, null, "image/jpeg", 5000, "PUBLIC");
        var primary = EsoProduct.Create("p2", "ADP.123", null, "https://x/file", "#this", null, 2, "application/x-fits", 200000, "PUBLIC");

        var selected = EsoProductSelectionPolicy.SelectBest([preview, primary]);

        Assert.Equal(primary, selected);
    }

    [Fact]
    public void EsoProductSelectionPolicy_PrefersHigherCalibrationLevel()
    {
        var raw = EsoProduct.Create("p1", "ADP.123", null, "https://x/raw.fits", "#this", null, 0, "application/x-fits", 100, "PUBLIC");
        var reduced = EsoProduct.Create("p2", "ADP.123", null, "https://x/reduced.fits", "#this", null, 2, "application/x-fits", 100, "PUBLIC");

        var selected = EsoProductSelectionPolicy.SelectBest([raw, reduced]);

        Assert.Equal(reduced, selected);
    }

    [Fact]
    public void EsoProductSelectionPolicy_EmptyList_ReturnsNull()
    {
        Assert.Null(EsoProductSelectionPolicy.SelectBest([]));
    }

    private sealed class StubEsoArchiveApiClient : IEsoArchiveApiClient
    {
        private readonly Func<string, Task<Result<IReadOnlyList<EsoProduct>>>> _getProducts;

        public StubEsoArchiveApiClient(Func<string, Task<Result<IReadOnlyList<EsoProduct>>>> getProducts)
        {
            _getProducts = getProducts;
        }

        public Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(
            ArchiveSearchQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<EsoProduct>>> GetProductsAsync(
            string datasetId, CancellationToken cancellationToken = default) =>
            _getProducts(datasetId);
    }

    private sealed class StubEsoArchiveDownloadClient : IEsoArchiveDownloadClient
    {
        private readonly Func<EsoProduct, CancellationToken, Task<Result<ArchiveDownload>>> _download;

        public StubEsoArchiveDownloadClient(Func<EsoProduct, CancellationToken, Task<Result<ArchiveDownload>>> download)
        {
            _download = download;
        }

        public List<EsoProduct> DownloadedProducts { get; } = [];

        public Task<Result<ArchiveDownload>> DownloadAsync(EsoProduct product, CancellationToken cancellationToken = default)
        {
            DownloadedProducts.Add(product);
            return _download(product, cancellationToken);
        }
    }
}
