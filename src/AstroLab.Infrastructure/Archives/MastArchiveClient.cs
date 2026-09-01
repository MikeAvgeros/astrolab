using System.IO.Pipelines;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AstroLab.Core.Result;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.Archives;

public sealed class MastArchiveClient : IMastArchiveClient
{
    private const string SearchEndpoint = "api/v0/invoke";
    private const string DownloadEndpoint = "api/v0/download/file";
    private const string CaomFilteredService = "Mast.Caom.Filtered";
    private const string CompleteStatus = "COMPLETE";
    private const string TargetNameParam = "target_name";
    private const string InstrumentNameParam = "instrument_name";
    private const string MinParam = "t_min";
    private const string UnknownInstrument = "UNKNOWN";
    private const string DefaultDatasetProductTemplate = "mast:HST/product/{0}/{0}_raw.fits";
    private const string MastUriPrefix = "mast:";

    private readonly HttpClient _httpClient;
    private readonly ILogger<MastArchiveClient> _logger;

    public MastArchiveClient(HttpClient httpClient, ILogger<MastArchiveClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ArchiveSource Source => ArchiveSource.Mast;

    public async Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(
        ArchiveSearchQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Target))
        {
            return Result<IReadOnlyList<ArchiveObservation>>.Failure(
                Error.Validation("mast.invalid_target", "Search target name must be provided."));
        }

        _logger.LogInformation("Executing MAST archive search for target '{Target}'", query.Target);

        var requestPayload = new MastMashupRequest
        {
            Service = CaomFilteredService,
            Format = "json",
            Params = new MastMashupParams
            {
                Columns = "*",
                Filters = BuildFilters(query),
                PageSize = query.MaxResults
            }
        };

        try
        {
            var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("request",
                    JsonSerializer.Serialize(requestPayload, MastJsonContext.Default.MastMashupRequest))
            ]);

            using var response = await _httpClient.PostAsync(SearchEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MAST search API request failed with HTTP status {StatusCode}", response.StatusCode);
                return Result<IReadOnlyList<ArchiveObservation>>.Failure(
                    Error.Unexpected("mast.search_http_error", $"MAST API returned HTTP status {(int)response.StatusCode}."));
            }

            var mashupResponse =
                await response.Content.ReadFromJsonAsync(MastJsonContext.Default.MastMashupResponse, cancellationToken);

            if (mashupResponse is null || mashupResponse.Status != CompleteStatus)
            {
                _logger.LogWarning("MAST search returned non-complete status: {Status}, Message: {Message}",
                    mashupResponse?.Status, mashupResponse?.Msg);

                return Result<IReadOnlyList<ArchiveObservation>>.Failure(
                    Error.Unexpected("mast.search_failed", mashupResponse?.Msg ?? "Failed to parse MAST search response."));
            }

            var observations = new List<ArchiveObservation>();

            foreach (var item in mashupResponse.Data)
            {
                if (string.IsNullOrWhiteSpace(item.ObsId))
                {
                    continue;
                }

                var target = string.IsNullOrWhiteSpace(item.TargetName) ? query.Target : item.TargetName;

                var instrument = string.IsNullOrWhiteSpace(item.InstrumentName) ? UnknownInstrument : item.InstrumentName;

                var obsDate = ModifiedJulianDate.ToDateTimeOffset(item.Min);

                observations.Add(ArchiveObservation.Create(item.ObsId, target, instrument, obsDate, ArchiveSource.Mast));
            }

            _logger.LogInformation("Successfully retrieved {Count} observations for target '{Target}'", observations.Count, query.Target);

            return Result<IReadOnlyList<ArchiveObservation>>.Success(observations);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("MAST search query was canceled for target '{Target}'", query.Target);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during MAST archive search for target '{Target}'", query.Target);
            return Result<IReadOnlyList<ArchiveObservation>>.Failure(
                Error.Unexpected("mast.search_unexpected_error", "An unexpected error occurred while searching MAST archive."));
        }
    }

    public async Task<Result<ArchiveDownload>> DownloadAsync(string datasetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(datasetId))
        {
            return Result<ArchiveDownload>.Failure(
                Error.Validation("mast.invalid_dataset_id", "datasetId must not be empty."));
        }

        _logger.LogInformation("Initiating download for MAST dataset {DatasetId}", datasetId);

        var uriQuery = datasetId.StartsWith(MastUriPrefix, StringComparison.OrdinalIgnoreCase)
            ? datasetId
            : string.Format(DefaultDatasetProductTemplate, datasetId);

        var requestUri = $"{DownloadEndpoint}?uri={Uri.EscapeDataString(uriQuery)}";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Dataset {DatasetId} was not found on MAST", datasetId);
                response.Dispose();
                return Result<ArchiveDownload>.Failure(
                    Error.NotFound("mast.dataset_not_found", $"Dataset '{datasetId}' was not found in MAST archive."));
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Download failed for dataset {DatasetId} with HTTP status {StatusCode}", datasetId, response.StatusCode);
                response.Dispose();
                return Result<ArchiveDownload>.Failure(
                    Error.Unexpected("mast.download_http_error", $"MAST download service returned HTTP {(int)response.StatusCode}."));
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var pipeReader = PipeReader.Create(responseStream);

            var fileName = ExtractFileName(response.Content.Headers, datasetId);

            var contentLength = response.Content.Headers.ContentLength;

            _logger.LogInformation("Successfully opened stream for dataset {DatasetId} ({FileName}, Content-Length: {ContentLength} bytes)",
                datasetId, fileName, contentLength);

            return Result<ArchiveDownload>.Success(new ArchiveDownload(fileName, contentLength, pipeReader, response));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Download canceled for dataset {DatasetId}", datasetId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during download for dataset {DatasetId}", datasetId);
            return Result<ArchiveDownload>.Failure(
                Error.Unexpected("mast.download_unexpected_error", "An unexpected error occurred while downloading dataset."));
        }
    }

    private static List<MastMashupFilter> BuildFilters(ArchiveSearchQuery query)
    {
        var filters = new List<MastMashupFilter>
        {
            new()
            {
                ParamName = TargetNameParam,
                Values = [MastFilterValue.FromText(query.Target!)]
            }
        };

        if (!string.IsNullOrWhiteSpace(query.Instrument))
        {
            filters.Add(new MastMashupFilter
            {
                ParamName = InstrumentNameParam,
                Values = [MastFilterValue.FromText(query.Instrument)]
            });
        }

        if (query.From is not null || query.To is not null)
        {
            var min = query.From is { } from ? ModifiedJulianDate.FromDateTimeOffset(from) : double.MinValue;
            
            var max = query.To is { } to ? ModifiedJulianDate.FromDateTimeOffset(to) : double.MaxValue;

            filters.Add(new MastMashupFilter
            {
                ParamName = MinParam,
                Values = [MastFilterValue.FromRange(min, max)]
            });
        }

        return filters;
    }

    private static string ExtractFileName(HttpContentHeaders headers, string fallbackDatasetId)
    {
        if (headers.ContentDisposition?.FileName is { } fileName)
        {
            return fileName.Trim('"');
        }

        return $"{fallbackDatasetId}.fits";
    }
}
