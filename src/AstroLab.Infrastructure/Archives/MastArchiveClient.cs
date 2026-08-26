using AstroLab.Core.Result;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.Archives;

/// <summary>
/// Initial integration with the Mikulski Archive for Space Telescopes (MAST). As with
/// <see cref="EsoArchiveClient"/>, the archive's real query/download surface (MAST's CAOM/VO
/// catalogue interface) is not yet wired up. This stub establishes the same resilient
/// <see cref="HttpClient"/> plumbing so the concrete implementation can be filled in
/// independently, without touching callers, <c>AstroLab.Core</c>, or the API feature slices.
/// </summary>
/// <remarks>
/// Deliberately never issues a request against a guessed URL on the real archive host: doing so
/// risks a coincidental 2xx response (e.g. a redirect or landing page) being silently reported as
/// "search succeeded, zero results" instead of the honest <see cref="ErrorCategory.NotImplemented"/>
/// this returns today. Once MAST's real endpoints are known, replace the bodies below with the
/// actual <see cref="_httpClient"/> request/response mapping.
/// </remarks>
public sealed class MastArchiveClient : IMastArchiveClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MastArchiveClient> _logger;

    public MastArchiveClient(HttpClient httpClient, ILogger<MastArchiveClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public ArchiveSource Source => ArchiveSource.Mast;

    public Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(
        ArchiveSearchQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MAST archive search requested for target {Target}, but MAST search is not implemented yet.", query.Target);

        return Task.FromResult(Result<IReadOnlyList<ArchiveObservation>>.Failure(
            Error.NotImplemented("mast.search_not_implemented", "MAST archive search is not yet implemented.")));
    }

    public Task<Result<ArchiveDownload>> DownloadAsync(string datasetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(datasetId))
        {
            return Task.FromResult(Result<ArchiveDownload>.Failure(
                Error.Validation("mast.invalid_dataset_id", "datasetId must not be empty.")));
        }

        _logger.LogInformation(
            "MAST archive download requested for dataset {DatasetId}, but MAST download is not implemented yet.", datasetId);

        return Task.FromResult(Result<ArchiveDownload>.Failure(
            Error.NotImplemented("mast.download_not_implemented", "MAST archive download is not yet implemented.")));
    }
}
