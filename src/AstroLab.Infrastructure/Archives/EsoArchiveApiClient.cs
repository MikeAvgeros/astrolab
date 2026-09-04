using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Web;
using AstroLab.Core.Result;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.Archives;

/// <summary>
/// Calls ESO's real IVOA TAP service (ADQL over <c>ivoa.ObsCore</c>) for observation search and
/// DataLink for product discovery — the metadata/query half of the ESO archive integration, split
/// from <see cref="EsoArchiveDownloadClient"/> so a large FITS transfer never inherits a
/// short-lived resilience policy sized for metadata requests.
/// </summary>
public sealed class EsoArchiveApiClient : IEsoArchiveApiClient
{
    private const string TapEndpoint = "tap_obs/sync";
    private const string DataLinkEndpoint = "datalink/links";
    private const string DatasetIdIvoPrefix = "ivo://eso.org/csp#";
    private const string UnknownInstrument = "UNKNOWN";

    private const string RequestedColumns =
        "dp_id,target_name,obs_collection,instrument_name,dataproduct_type,calib_level," +
        "t_min,t_max,t_exptime,s_ra,s_dec,em_min,em_max,proposal_id,obs_creator_name,data_rights";

    private readonly HttpClient _httpClient;
    private readonly ILogger<EsoArchiveApiClient> _logger;

    public EsoArchiveApiClient(HttpClient httpClient, ILogger<EsoArchiveApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

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
                return Result<IReadOnlyList<ArchiveObservation>>.Failure(
                    await MapHttpErrorAsync(response, "eso.search", cancellationToken));
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
            _logger.LogWarning("ESO search query was canceled for target '{Target}'", query.Target);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during ESO archive search for target {Target}", query.Target);
            return Result<IReadOnlyList<ArchiveObservation>>.Failure(Error.Unexpected("eso.search_exception", ex.Message));
        }
    }

    public async Task<Result<IReadOnlyList<EsoProduct>>> GetProductsAsync(
        string datasetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(datasetId))
        {
            return Result<IReadOnlyList<EsoProduct>>.Failure(
                Error.Validation("eso.invalid_dataset_id", "datasetId must not be empty."));
        }

        var requestUri = $"{DataLinkEndpoint}?ID={DatasetIdIvoPrefix}{HttpUtility.UrlEncode(datasetId)}&RESPONSEFORMAT=json";

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result<IReadOnlyList<EsoProduct>>.Failure(
                    await MapHttpErrorAsync(response, "eso.products", cancellationToken));
            }

            var dataLinkResponse = await response.Content.ReadFromJsonAsync<EsoTapResponse>(cancellationToken: cancellationToken);

            var products = MapDataLinkResponseToProducts(dataLinkResponse, datasetId);

            return Result<IReadOnlyList<EsoProduct>>.Success(products);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("ESO product discovery was canceled for dataset {DatasetId}", datasetId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during ESO product discovery for dataset {DatasetId}", datasetId);
            return Result<IReadOnlyList<EsoProduct>>.Failure(Error.Unexpected("eso.products_exception", ex.Message));
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
            conditions.Add($"t_max >= {ModifiedJulianDate.FromDateTimeOffset(from).ToString(CultureInfo.InvariantCulture)}");
        }

        if (query.To is { } to)
        {
            conditions.Add($"t_min <= {ModifiedJulianDate.FromDateTimeOffset(to).ToString(CultureInfo.InvariantCulture)}");
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        return $"SELECT TOP {query.MaxResults} {RequestedColumns} FROM ivoa.ObsCore {whereClause}";
    }

    private static string EscapeAdqlLiteral(string value) => value.Replace("'", "''");

    private static Dictionary<string, int> BuildColumnIndex(List<EsoColumnMetadata> metadata) =>
        metadata
            .Select((col, idx) => (col.Name.ToLowerInvariant(), idx))
            .ToDictionary(x => x.Item1, x => x.idx);

    private static List<ArchiveObservation> MapResponseToObservations(EsoTapResponse response)
    {
        var observations = new List<ArchiveObservation>();

        if (response.Metadata is null || response.Data is null)
        {
            return observations;
        }

        var columnIndex = BuildColumnIndex(response.Metadata);

        foreach (var rowValues in response.Data)
        {
            var row = new EsoTapRow(columnIndex, rowValues);

            var datasetId = row.GetString("dp_id");
            if (string.IsNullOrWhiteSpace(datasetId))
            {
                continue;
            }

            var target = row.GetString("target_name");
            if (string.IsNullOrWhiteSpace(target))
            {
                continue;
            }

            var instrument = row.GetString("instrument_name");
            if (string.IsNullOrWhiteSpace(instrument))
            {
                instrument = UnknownInstrument;
            }

            observations.Add(ArchiveObservation.Create(
                datasetId, target, instrument, ModifiedJulianDate.ToDateTimeOffset(row.GetDouble("t_min")), ArchiveSource.Eso,
                collection: row.GetString("obs_collection"),
                dataProductType: row.GetString("dataproduct_type"),
                calibrationLevel: row.GetInt("calib_level"),
                rightAscension: row.GetDouble("s_ra"),
                declination: row.GetDouble("s_dec"),
                exposureTimeSeconds: row.GetDouble("t_exptime"),
                wavelengthMinMicrometres: row.GetDouble("em_min"),
                wavelengthMaxMicrometres: row.GetDouble("em_max"),
                proposalId: row.GetString("proposal_id"),
                proposalPi: row.GetString("obs_creator_name"),
                dataRights: row.GetString("data_rights")));
        }

        return observations;
    }

    private static List<EsoProduct> MapDataLinkResponseToProducts(EsoTapResponse? response, string datasetId)
    {
        var products = new List<EsoProduct>();

        if (response?.Metadata is null || response.Data is null)
        {
            return products;
        }

        var columnIndex = BuildColumnIndex(response.Metadata);

        foreach (var rowValues in response.Data)
        {
            var row = new EsoTapRow(columnIndex, rowValues);

            var errorMessage = row.GetString("error_message");
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                continue;
            }

            var dataUri = row.GetString("access_url");
            if (string.IsNullOrWhiteSpace(dataUri))
            {
                continue;
            }

            var id = row.GetString("id") ?? datasetId;

            products.Add(EsoProduct.Create(
                id: id,
                observationId: datasetId,
                fileName: null,
                dataUri: dataUri,
                productType: row.GetString("semantics"),
                dataProductType: null,
                calibrationLevel: null,
                format: row.GetString("content_type"),
                size: row.GetLong("content_length"),
                dataRights: null));
        }

        return products;
    }

    private async Task<Error> MapHttpErrorAsync(HttpResponseMessage response, string errorCodePrefix, CancellationToken cancellationToken)
    {
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogWarning("ESO API request failed with HTTP status {StatusCode}: {Error}", response.StatusCode, errorContent);

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => Error.Validation($"{errorCodePrefix}.invalid_request", "ESO rejected the request as invalid."),
            HttpStatusCode.Unauthorized => Error.Unauthorized($"{errorCodePrefix}.unauthorized", "ESO request was not authorized."),
            HttpStatusCode.Forbidden => Error.Unauthorized($"{errorCodePrefix}.forbidden", "ESO request was forbidden."),
            HttpStatusCode.NotFound => Error.NotFound($"{errorCodePrefix}.not_found", "The requested ESO resource was not found."),
            HttpStatusCode.TooManyRequests => Error.Infrastructure($"{errorCodePrefix}.rate_limited", "ESO rate-limited this request."),
            _ when (int)response.StatusCode >= 500 => Error.Infrastructure(
                $"{errorCodePrefix}.upstream_error", $"ESO returned an upstream error (HTTP {(int)response.StatusCode})."),
            _ => Error.Unexpected($"{errorCodePrefix}.http_error", $"ESO API returned HTTP status {(int)response.StatusCode}."),
        };
    }
}
