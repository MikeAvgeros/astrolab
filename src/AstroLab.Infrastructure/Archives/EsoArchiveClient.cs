using AstroLab.Core.Result;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.Archives;

/// <summary>
/// Initial integration with the ESO Science Archive Facility. The archive's real query/download
/// surface (a TAP/VO ADQL interface for metadata, and per-dataset download endpoints) is not yet
/// wired up. This stub establishes the resilient <see cref="HttpClient"/> plumbing (resolved via
/// <see cref="IHttpClientFactory"/>, retried/circuit-broken centrally in DI — see
/// <c>InfrastructureServiceCollectionExtensions</c>) so the real request/response contracts can be
/// filled in later without touching callers, <c>AstroLab.Core</c>, or the API feature slices.
/// </summary>
/// <remarks>
/// Deliberately never issues a request against a guessed URL on the real archive host: doing so
/// risks a coincidental 2xx response (e.g. a redirect or landing page) being silently reported as
/// "search succeeded, zero results" instead of the honest <see cref="ErrorCategory.NotImplemented"/>
/// this returns today. Once ESO's real endpoints are known, replace the bodies below with the
/// actual <see cref="_httpClient"/> request/response mapping.
/// </remarks>
public sealed class EsoArchiveClient : IEsoArchiveClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EsoArchiveClient> _logger;

    public EsoArchiveClient(HttpClient httpClient, ILogger<EsoArchiveClient> logger)
    {
        _httpClient = httpClient;

        _logger = logger;
    }

    public ArchiveSource Source => ArchiveSource.Eso;

    public Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(
        ArchiveSearchQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "ESO archive search requested for target {Target}, but ESO search is not implemented yet.", query.Target);

        return Task.FromResult(Result<IReadOnlyList<ArchiveObservation>>.Failure(
            Error.NotImplemented("eso.search_not_implemented", "ESO archive search is not yet implemented.")));
    }

    public Task<Result<ArchiveDownload>> DownloadAsync(string datasetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(datasetId))
        {
            return Task.FromResult(Result<ArchiveDownload>.Failure(
                Error.Validation("eso.invalid_dataset_id", "datasetId must not be empty.")));
        }

        _logger.LogInformation(
            "ESO archive download requested for dataset {DatasetId}, but ESO download is not implemented yet.", datasetId);

        return Task.FromResult(Result<ArchiveDownload>.Failure(
            Error.NotImplemented("eso.download_not_implemented", "ESO archive download is not yet implemented.")));
    }
}
