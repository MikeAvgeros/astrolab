using System.IO.Pipelines;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.Archives;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.MAST;

/// <summary>
/// Initial integration with the Mikulski Archive for Space Telescopes (MAST). As with
/// <see cref="AstroLab.Infrastructure.ESO.EsoArchiveClient"/>, the archive's real query/download
/// surface (MAST's CAOM/VO catalogue interface) is not yet wired up — this stub establishes the
/// same resilient HTTP plumbing so the concrete implementation can be filled in independently,
/// without touching callers, <c>AstroLab.Core</c>, or the API feature slices.
/// </summary>
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
                    "mast.search_failed", $"MAST archive search failed with status {(int)response.StatusCode}.");
            }

            _logger.LogInformation("MAST archive search stub invoked for target {Target}.", query.Target);
            return Result<IReadOnlyList<ArchiveObservation>>.Success(Array.Empty<ArchiveObservation>());
        }
        catch (HttpRequestException ex)
        {
            return Error.Infrastructure("mast.search_unreachable", $"Could not reach the MAST archive: {ex.Message}");
        }
    }

    public async Task<Result<ArchiveDownload>> DownloadAsync(string datasetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(datasetId))
        {
            return Error.Validation("mast.invalid_dataset_id", "datasetId must not be empty.");
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
                return Error.Infrastructure("mast.download_failed", $"MAST archive download failed with status {status}.");
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var reader = PipeReader.Create(stream);
            return new ArchiveDownload($"{datasetId}.fits", response.Content.Headers.ContentLength, reader, response);
        }
        catch (HttpRequestException ex)
        {
            response?.Dispose();
            return Error.Infrastructure("mast.download_unreachable", $"Could not reach the MAST archive: {ex.Message}");
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

        return new Uri($"api/v0/invoke?{string.Join('&', parameters)}", UriKind.Relative);
    }

    private static Uri BuildDownloadUri(string datasetId) =>
        new($"api/v0.1/Download/file?uri={Uri.EscapeDataString(datasetId)}", UriKind.Relative);
}
