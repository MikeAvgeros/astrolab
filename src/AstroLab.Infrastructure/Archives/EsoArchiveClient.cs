using AstroLab.Core.Result;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.Archives;

/// <summary>
/// Application-facing ESO archive client. Thin orchestrator that delegates search/product
/// discovery to <see cref="IEsoArchiveApiClient"/> and streamed FITS downloads to
/// <see cref="IEsoArchiveDownloadClient"/>, and implements the dataset-id download convenience
/// overload by looking up products, picking one via <see cref="EsoProductSelectionPolicy"/>, then
/// downloading it.
/// </summary>
public sealed class EsoArchiveClient : IEsoArchiveClient
{
    private readonly IEsoArchiveApiClient _apiClient;
    private readonly IEsoArchiveDownloadClient _downloadClient;
    private readonly ILogger<EsoArchiveClient> _logger;

    public EsoArchiveClient(IEsoArchiveApiClient apiClient, IEsoArchiveDownloadClient downloadClient, ILogger<EsoArchiveClient> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _downloadClient = downloadClient ?? throw new ArgumentNullException(nameof(downloadClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ArchiveSource Source => ArchiveSource.Eso;

    public Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(
        ArchiveSearchQuery query, CancellationToken cancellationToken = default) =>
        _apiClient.SearchAsync(query, cancellationToken);

    public Task<Result<IReadOnlyList<EsoProduct>>> GetProductsAsync(
        string datasetId, CancellationToken cancellationToken = default) =>
        _apiClient.GetProductsAsync(datasetId, cancellationToken);

    public async Task<Result<ArchiveDownload>> DownloadAsync(string datasetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(datasetId))
        {
            return Result<ArchiveDownload>.Failure(
                Error.Validation("eso.invalid_dataset_id", "datasetId must not be empty."));
        }

        var productsResult = await GetProductsAsync(datasetId, cancellationToken);

        if (productsResult.IsFailure)
        {
            return Result<ArchiveDownload>.Failure(productsResult.Error);
        }

        var selected = EsoProductSelectionPolicy.SelectBest(productsResult.Value);

        if (selected is not null) return await DownloadAsync(selected, cancellationToken);

        _logger.LogWarning("No suitable product found for ESO dataset {DatasetId}", datasetId);

        return Result<ArchiveDownload>.Failure(
            Error.NotFound("eso.no_suitable_product", $"No suitable product was found for dataset '{datasetId}'."));
    }

    public Task<Result<ArchiveDownload>> DownloadAsync(EsoProduct product, CancellationToken cancellationToken = default) =>
        _downloadClient.DownloadAsync(product, cancellationToken);
}
