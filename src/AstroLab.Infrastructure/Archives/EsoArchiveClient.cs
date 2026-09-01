using System.Globalization;
using System.IO.Pipelines;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using AstroLab.Core.Result;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.Archives;

public sealed class EsoArchiveClient : IEsoArchiveClient
{
    private const string TapEndpoint = "tap_obs/sync";
    private const string DataLinkEndpoint = "datalink/links";
    private const string DatasetIdIvoPrefix = "ivo://eso.org/csp#";
    private const string UnknownInstrument = "UNKNOWN";

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
            var adqlQuery = BuildAdqlQuery(query);

            var formContent = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("REQUEST", "doQuery"),
                new KeyValuePair<string, string>("LANG", "ADQL"),
                new KeyValuePair<string, string>("FORMAT", "json"),
                new KeyValuePair<string, string>("QUERY", adqlQuery)
            ]);

            using var response = await _httpClient.PostAsync(TapEndpoint, formContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogWarning("ESO TAP search failed with status {StatusCode}: {Error}", response.StatusCode,
                    errorContent);

                return Result<IReadOnlyList<ArchiveObservation>>.Failure(
                    Error.NotFound("eso.search_not_found",
                        $"ESO archive API returned HTTP status {(int)response.StatusCode}."));
            }

            var tapResponse = await response.Content.ReadFromJsonAsync<EsoTapResponse>(cancellationToken: cancellationToken);

            if (tapResponse?.Data is null || tapResponse.Data.Count == 0)
            {
                return Result<IReadOnlyList<ArchiveObservation>>.Success(Array.Empty<ArchiveObservation>());
            }

            var observations = MapResponseToObservations(tapResponse);

            return Result<IReadOnlyList<ArchiveObservation>>.Success(observations);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("ESO search query was canceled for target '{Target}'", query.Target);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during ESO archive search for target {Target}", query.Target);
            return Result<IReadOnlyList<ArchiveObservation>>.Failure(Error.Unexpected("eso.search_exception", ex.Message));
        }
    }

    public async Task<Result<ArchiveDownload>> DownloadAsync(string datasetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(datasetId))
        {
            return Result<ArchiveDownload>.Failure(
                Error.Validation("eso.invalid_dataset_id", "datasetId must not be empty."));
        }

        var downloadUrl = $"{DataLinkEndpoint}?ID={DatasetIdIvoPrefix}{HttpUtility.UrlEncode(datasetId)}";

        try
        {
            var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogWarning("ESO download failed for dataset {DatasetId} with status {StatusCode}: {Error}",
                    datasetId, response.StatusCode, errorContent);
                
                response.Dispose();

                return Result<ArchiveDownload>.Failure(
                    Error.NotFound("eso.dataset_not_found", $"Dataset '{datasetId}' could not be retrieved from ESO."));
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            
            var pipeReader = PipeReader.Create(responseStream);
            
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"{datasetId}.fits";
            
            var contentLength = response.Content.Headers.ContentLength;

            return Result<ArchiveDownload>.Success(new ArchiveDownload(fileName, contentLength, pipeReader, response));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("ESO download was canceled for dataset {DatasetId}", datasetId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while downloading ESO dataset {DatasetId}", datasetId);
            return Result<ArchiveDownload>.Failure(
                Error.Unexpected("eso.download_exception", ex.Message));
        }
    }

    private static string BuildAdqlQuery(ArchiveSearchQuery query)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Target))
        {
            conditions.Add($"target_name LIKE '%{EscapeAdqlLiteral(query.Target)}%'");
        }

        if (!string.IsNullOrWhiteSpace(query.Instrument))
        {
            conditions.Add($"instrument_name = '{EscapeAdqlLiteral(query.Instrument)}'");
        }

        if (query.From is { } from)
        {
            conditions.Add($"t_min >= {ModifiedJulianDate.FromDateTimeOffset(from).ToString(CultureInfo.InvariantCulture)}");
        }

        if (query.To is { } to)
        {
            conditions.Add($"t_min <= {ModifiedJulianDate.FromDateTimeOffset(to).ToString(CultureInfo.InvariantCulture)}");
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        return $"SELECT TOP {query.MaxResults} dp_id, target_name, obs_id, instrument_name, t_min, t_exptime " +
               $"FROM ivoa.ObsCore {whereClause}";
    }

    private static string EscapeAdqlLiteral(string value) => value.Replace("'", "''");

    private static List<ArchiveObservation> MapResponseToObservations(EsoTapResponse response)
    {
        var observations = new List<ArchiveObservation>();
        if (response.Metadata is null || response.Data is null) return observations;

        var columns = response.Metadata
            .Select((col, idx) => (col.Name.ToLowerInvariant(), idx))
            .ToDictionary(x => x.Item1, x => x.idx);

        foreach (var row in response.Data)
        {
            string GetValue(string colName)
            {
                if (!columns.TryGetValue(colName, out var idx) || idx >= row.Count) return string.Empty;

                return row[idx] switch
                {
                    JsonElement { ValueKind: JsonValueKind.String } elem => elem.GetString() ?? string.Empty,
                    JsonElement elem => elem.ToString(),
                    _ => row[idx].ToString() ?? string.Empty
                };
            }

            var datasetId = GetValue("dp_id");
            if (string.IsNullOrWhiteSpace(datasetId))
            {
                continue;
            }

            var target = GetValue("target_name");
            if (string.IsNullOrWhiteSpace(target))
            {
                continue;
            }

            var instrument = GetValue("instrument_name");
            if (string.IsNullOrWhiteSpace(instrument))
            {
                instrument = UnknownInstrument;
            }

            var mjd = double.TryParse(GetValue("t_min"), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedMjd)
                ? parsedMjd
                : (double?)null;

            observations.Add(ArchiveObservation.Create(
                datasetId,
                target,
                instrument,
                ModifiedJulianDate.ToDateTimeOffset(mjd),
                ArchiveSource.Eso));
        }

        return observations;
    }
}
