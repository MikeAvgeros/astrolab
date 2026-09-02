using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AstroLab.Core.Result;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.Archives;

public sealed class MastArchiveApiClient : IMastArchiveApiClient
{
    private const string InvokeEndpoint = "api/v0/invoke";
    private const string NameLookupService = "Mast.Name.Lookup";
    private const string CaomFilteredService = "Mast.Caom.Filtered";
    private const string ProductsService = "Mast.Caom.Products";
    private const string CompleteStatus = "COMPLETE";
    private const string CollectionParam = "obs_collection";
    private const string InstrumentNameParam = "instrument_name";
    private const string MinParam = "t_min";
    private const string UnknownInstrument = "UNKNOWN";
    private const string RequestFormFieldName = "request";

    private const string RequestedColumns =
        "obsid,obs_id,target_name,obs_collection,instrument_name,dataproduct_type,calib_level," +
        "t_min,t_max,t_exptime,s_ra,s_dec,em_min,em_max,proposal_id,proposal_pi,data_rights";

    private readonly HttpClient _httpClient;
    private readonly ILogger<MastArchiveApiClient> _logger;

    public MastArchiveApiClient(HttpClient httpClient, ILogger<MastArchiveApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<MastTarget>> ResolveTargetAsync(string target, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return Result<MastTarget>.Failure(
                Error.Validation("mast.invalid_target", "Target name must be provided."));
        }

        var requestPayload = new MastNameLookupRequest
        {
            Service = NameLookupService,
            Params = new MastNameLookupParams { Input = target }
        };

        try
        {
            var content = BuildRequestContent(
                JsonSerializer.Serialize(requestPayload, MastJsonContext.Default.MastNameLookupRequest));

            using var response = await _httpClient.PostAsync(InvokeEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result<MastTarget>.Failure(await MapHttpErrorAsync(response, "mast.resolve", cancellationToken));
            }

            var lookupResponse =
                await response.Content.ReadFromJsonAsync(MastJsonContext.Default.MastNameLookupResponse, cancellationToken);

            var resolved = lookupResponse?.ResolvedCoordinate
                .FirstOrDefault(c => c.RightAscension is not null && c.Declination is not null);

            if (resolved is not null)
                return Result<MastTarget>.Success(
                    new MastTarget(resolved.CanonicalName ?? target, resolved.RightAscension!.Value,
                        resolved.Declination!.Value));

            _logger.LogWarning("MAST could not resolve target '{Target}' to sky coordinates", target);

            return Result<MastTarget>.Failure(
                Error.NotFound("mast.target_not_resolved", $"Could not resolve target '{target}' to sky coordinates."));

        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("MAST target resolution was canceled for target '{Target}'", target);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error resolving MAST target '{Target}'", target);
            return Result<MastTarget>.Failure(
                Error.Unexpected("mast.resolve_unexpected_error", "An unexpected error occurred while resolving the target name."));
        }
    }

    public async Task<Result<IReadOnlyList<ArchiveObservation>>> SearchAsync(
        ArchiveSearchQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Target))
        {
            return Result<IReadOnlyList<ArchiveObservation>>.Failure(
                Error.Validation("mast.invalid_target", "Search target name must be provided."));
        }

        if (query is { From: { } fromBound, To: { } toBound } && fromBound > toBound)
        {
            return Result<IReadOnlyList<ArchiveObservation>>.Failure(
                Error.Validation("mast.invalid_date_range", "'From' must not be later than 'To'."));
        }

        var targetResult = await ResolveTargetAsync(query.Target, cancellationToken);

        if (targetResult.IsFailure)
        {
            return Result<IReadOnlyList<ArchiveObservation>>.Failure(targetResult.Error);
        }

        var target = targetResult.Value;

        var requestPayload = new MastMashupRequest
        {
            Service = CaomFilteredService,
            Params = new MastMashupParams
            {
                Columns = RequestedColumns,
                Filters = BuildFilters(query),
                Position = FormattableString.Invariant($"{target.RightAscension}, {target.Declination}"),
                Radius = query.SearchRadiusDegrees,
                PageSize = query.MaxResults
            }
        };

        try
        {
            var requestJson = JsonSerializer.Serialize(requestPayload, MastJsonContext.Default.MastMashupRequest);

            var content = BuildRequestContent(requestJson);

            using var response = await _httpClient.PostAsync(InvokeEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result<IReadOnlyList<ArchiveObservation>>.Failure(
                    await MapHttpErrorAsync(response, "mast.search", cancellationToken));
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

                observations.Add(MapToArchiveObservation(ToMastObservation(item), query.Target));
            }

            return Result<IReadOnlyList<ArchiveObservation>>.Success(observations);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("MAST search query was canceled for target '{Target}'", query.Target);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during MAST archive search for target '{Target}'", query.Target);
            return Result<IReadOnlyList<ArchiveObservation>>.Failure(
                Error.Unexpected("mast.search_unexpected_error", "An unexpected error occurred while searching MAST archive."));
        }
    }

    public async Task<Result<IReadOnlyList<MastProduct>>> GetProductsAsync(
        string observationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(observationId))
        {
            return Result<IReadOnlyList<MastProduct>>.Failure(
                Error.Validation("mast.invalid_observation_id", "observationId must not be empty."));
        }

        var requestPayload = new MastProductRequest
        {
            Service = ProductsService,
            Params = new MastProductParams { ObsId = observationId }
        };

        try
        {
            var content = BuildRequestContent(
                JsonSerializer.Serialize(requestPayload, MastJsonContext.Default.MastProductRequest));

            using var response = await _httpClient.PostAsync(InvokeEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result<IReadOnlyList<MastProduct>>.Failure(
                    await MapHttpErrorAsync(response, "mast.products", cancellationToken));
            }

            var productResponse =
                await response.Content.ReadFromJsonAsync(MastJsonContext.Default.MastProductResponse, cancellationToken);

            if (productResponse is null || productResponse.Status != CompleteStatus)
            {
                _logger.LogWarning("MAST products lookup returned non-complete status: {Status}, Message: {Message}",
                    productResponse?.Status, productResponse?.Msg);

                return Result<IReadOnlyList<MastProduct>>.Failure(
                    Error.Unexpected("mast.products_failed", productResponse?.Msg ?? "Failed to parse MAST products response."));
            }

            var products = new List<MastProduct>();

            foreach (var record in productResponse.Data)
            {
                if (string.IsNullOrWhiteSpace(record.DataUri))
                {
                    continue;
                }

                products.Add(new MastProduct(
                    record.DataUri, record.ProductFilename, record.ProductType, record.DataProductType,
                    record.CalibLevel, record.Size, record.DataRights));
            }

            return Result<IReadOnlyList<MastProduct>>.Success(products);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("MAST products lookup was canceled for observation {ObservationId}", observationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving MAST products for observation {ObservationId}", observationId);
            return Result<IReadOnlyList<MastProduct>>.Failure(
                Error.Unexpected("mast.products_unexpected_error", "An unexpected error occurred while retrieving products."));
        }
    }

    private static FormUrlEncodedContent BuildRequestContent(string requestJson) =>
        new([new KeyValuePair<string, string>(RequestFormFieldName, requestJson)]);

    private static List<MastMashupFilter> BuildFilters(ArchiveSearchQuery query)
    {
        var filters = new List<MastMashupFilter>();

        if (!string.IsNullOrWhiteSpace(query.Mission))
        {
            filters.Add(new MastMashupFilter
            {
                ParamName = CollectionParam,
                Values = [MastFilterValue.FromText(query.Mission)]
            });
        }

        if (!string.IsNullOrWhiteSpace(query.Instrument))
        {
            filters.Add(new MastMashupFilter
            {
                ParamName = InstrumentNameParam,
                Values = [MastFilterValue.FromText(query.Instrument)]
            });
        }

        if (query is { From: { } from, To: { } to })
        {
            filters.Add(new MastMashupFilter
            {
                ParamName = MinParam,
                Values =
                [
                    MastFilterValue.FromRange(ModifiedJulianDate.FromDateTimeOffset(from), ModifiedJulianDate.FromDateTimeOffset(to))
                ]
            });
        }
        else if (query.From is { } fromOnly)
        {
            filters.Add(new MastMashupFilter
            {
                ParamName = MinParam,
                Values = [MastFilterValue.FromMinBound(ModifiedJulianDate.FromDateTimeOffset(fromOnly))]
            });
        }
        else if (query.To is { } toOnly)
        {
            filters.Add(new MastMashupFilter
            {
                ParamName = MinParam,
                Values = [MastFilterValue.FromMaxBound(ModifiedJulianDate.FromDateTimeOffset(toOnly))]
            });
        }

        return filters;
    }

    private static MastObservation ToMastObservation(MastCaomRecord record) => new(
        ObsId: record.ObsId!,
        TargetName: record.TargetName,
        Collection: record.ObsCollection,
        Instrument: record.InstrumentName,
        DataProductType: record.DataProductType,
        CalibrationLevel: record.CalibLevel,
        ObservationStart: record.Min,
        ObservationEnd: record.Max,
        ExposureTime: record.ExposureTime,
        RightAscension: record.RightAscension,
        Declination: record.Declination,
        WavelengthMin: record.WavelengthMin,
        WavelengthMax: record.WavelengthMax,
        ProposalId: record.ProposalId,
        ProposalPi: record.ProposalPi,
        DataRights: record.DataRights);

    private static ArchiveObservation MapToArchiveObservation(MastObservation observation, string fallbackTarget)
    {
        var target = string.IsNullOrWhiteSpace(observation.TargetName) ? fallbackTarget : observation.TargetName;

        var instrument = string.IsNullOrWhiteSpace(observation.Instrument) ? UnknownInstrument : observation.Instrument;

        var obsDate = ModifiedJulianDate.ToDateTimeOffset(observation.ObservationStart);

        return ArchiveObservation.Create(
            observation.ObsId, target, instrument, obsDate, ArchiveSource.Mast,
            collection: observation.Collection,
            dataProductType: observation.DataProductType,
            calibrationLevel: observation.CalibrationLevel,
            rightAscension: observation.RightAscension,
            declination: observation.Declination,
            exposureTimeSeconds: observation.ExposureTime,
            wavelengthMinMicrometres: observation.WavelengthMin,
            wavelengthMaxMicrometres: observation.WavelengthMax,
            proposalId: observation.ProposalId,
            proposalPi: observation.ProposalPi,
            dataRights: observation.DataRights);
    }

    private async Task<Error> MapHttpErrorAsync(HttpResponseMessage response, string errorCodePrefix, CancellationToken cancellationToken)
    {
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogWarning("MAST API request failed with HTTP status {StatusCode}: {Error}", response.StatusCode, errorContent);

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => Error.Validation($"{errorCodePrefix}.invalid_request", "MAST rejected the request as invalid."),
            HttpStatusCode.Unauthorized => Error.Unauthorized($"{errorCodePrefix}.unauthorized", "MAST request was not authorized."),
            HttpStatusCode.Forbidden => Error.Unauthorized($"{errorCodePrefix}.forbidden", "MAST request was forbidden."),
            HttpStatusCode.NotFound => Error.NotFound($"{errorCodePrefix}.not_found", "The requested MAST resource was not found."),
            HttpStatusCode.TooManyRequests => Error.Infrastructure($"{errorCodePrefix}.rate_limited", "MAST rate-limited this request."),
            _ when (int)response.StatusCode >= 500 => Error.Infrastructure(
                $"{errorCodePrefix}.upstream_error", $"MAST returned an upstream error (HTTP {(int)response.StatusCode})."),
            _ => Error.Unexpected($"{errorCodePrefix}.http_error", $"MAST API returned HTTP status {(int)response.StatusCode}."),
        };
    }
}
