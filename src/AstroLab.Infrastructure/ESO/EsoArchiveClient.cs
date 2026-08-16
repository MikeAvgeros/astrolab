using System.IO.Pipelines;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.Archives;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.ESO;

/// <summary>
/// Initial integration with the ESO Science Archive Facility. The archive's real query/download
/// surface (a TAP/VO ADQL interface for metadata, and per-dataset download endpoints) is not yet
/// wired up — this stub establishes the HTTP plumbing (a resilient, retrying <see cref="HttpClient"/>
/// resolved via <see cref="IHttpClientFactory"/>) so the actual request/response contracts can be
/// filled in later without touching callers, <c>AstroLab.Core</c>, or the API feature slices.
/// </summary>
/// <remarks>
/// Retry, circuit-breaking, and timeout behaviour are configured once, centrally, on the
/// <see cref="HttpClient"/> registration itself (see the standard resilience handler wired up in
/// DI) rather than hand-rolled here — that keeps this class focused purely on request shaping and
/// response mapping.
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

    public async Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(
        ArchiveSearchQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildSearchUri(query));
            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Error.Infrastructure(
                    "eso.search_failed", $"ESO archive search failed with status {(int)response.StatusCode}.");
            }

            _logger.LogInformation("ESO archive search stub invoked for target {Target}.", query.Target);
            return Result<IReadOnlyList<ArchiveObservation>>.Success(Array.Empty<ArchiveObservation>());
        }
        catch (HttpRequestException ex)
        {
            return Error.Infrastructure("eso.search_unreachable", $"Could not reach the ESO archive: {ex.Message}");
        }
    }

    public async Task<Result<ArchiveDownload>> DownloadAsync(string datasetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(datasetId))
        {
            return Error.Validation("eso.invalid_dataset_id", "datasetId must not be empty.");
        }

        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient
                .GetAsync(BuildDownloadUri(datasetId), HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                response.Dispose();
                return Error.Infrastructure("eso.download_failed", $"ESO archive download failed with status {status}.");
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var reader = PipeReader.Create(stream);
            return new ArchiveDownload($"{datasetId}.fits", response.Content.Headers.ContentLength, reader, response);
        }
        catch (HttpRequestException ex)
        {
            response?.Dispose();
            return Error.Infrastructure("eso.download_unreachable", $"Could not reach the ESO archive: {ex.Message}");
        }
    }

    private static Uri BuildSearchUri(ArchiveSearchQuery query)
    {
        var parameters = new List<string>(4) { $"maxrec={query.MaxResults}" };
        if (!string.IsNullOrWhiteSpace(query.Target))
        {
            parameters.Add($"target={Uri.EscapeDataString(query.Target)}");
        }

        if (!string.IsNullOrWhiteSpace(query.Instrument))
        {
            parameters.Add($"instrument={Uri.EscapeDataString(query.Instrument)}");
        }

        return new Uri($"api/v1/search?{string.Join('&', parameters)}", UriKind.Relative);
    }

    private static Uri BuildDownloadUri(string datasetId) =>
        new($"api/v1/dataset/{Uri.EscapeDataString(datasetId)}", UriKind.Relative);
}
