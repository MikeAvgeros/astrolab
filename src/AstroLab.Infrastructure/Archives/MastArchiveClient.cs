using AstroLab.Core.Result;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.Archives;

public sealed class MastArchiveClient : IMastArchiveClient
{
    private readonly IMastArchiveApiClient _apiClient;
    private readonly IMastArchiveDownloadClient _downloadClient;
    private readonly ILogger<MastArchiveClient> _logger;

    public MastArchiveClient(IMastArchiveApiClient apiClient, IMastArchiveDownloadClient downloadClient, ILogger<MastArchiveClient> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _downloadClient = downloadClient ?? throw new ArgumentNullException(nameof(downloadClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ArchiveSource Source => ArchiveSource.Mast;

    public Task<Result<MastTarget>> ResolveTargetAsync(string target, CancellationToken cancellationToken = default) =>
        _apiClient.ResolveTargetAsync(target, cancellationToken);

    public Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(
        ArchiveSearchQuery query, CancellationToken cancellationToken = default) =>
        _apiClient.SearchAsync(query, cancellationToken);

    public Task<Result<IReadOnlyList<MastProduct>>> GetProductsAsync(
        string observationId, CancellationToken cancellationToken = default) =>
        _apiClient.GetProductsAsync(observationId, cancellationToken);

    public async Task<Result<ArchiveDownload>> DownloadAsync(string observationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(observationId))
        {
            return Result<ArchiveDownload>.Failure(
                Error.Validation("mast.invalid_dataset_id", "datasetId must not be empty."));
        }

        var productsResult = await GetProductsAsync(observationId, cancellationToken);

        if (productsResult.IsFailure)
        {
            return Result<ArchiveDownload>.Failure(productsResult.Error);
        }

        var selected = MastProductSelectionPolicy.SelectBest(productsResult.Value);

        if (selected is not null) return await DownloadAsync(selected, cancellationToken);

        _logger.LogWarning("No suitable FITS product found for MAST observation {ObservationId}", observationId);

        return Result<ArchiveDownload>.Failure(
            Error.NotFound("mast.no_suitable_product", $"No suitable FITS product was found for observation '{observationId}'."));
    }

    public Task<Result<ArchiveDownload>> DownloadAsync(MastProduct product, CancellationToken cancellationToken = default) =>
        _downloadClient.DownloadAsync(product, cancellationToken);
}
