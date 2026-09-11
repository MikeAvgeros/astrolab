using System.Globalization;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using AstroLab.Core.Result;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.Catalogues;

/// <summary>
/// Calls VizieR's real IVOA TAP service (ADQL over a named VizieR table, e.g.
/// <c>I/355/gaiadr3</c>) for spatial cone-search queries.
///
/// A VizieR table's own column names vary per catalogue, so which columns carry RA/Dec, a source
/// identifier, and a magnitude is discovered per catalogue from the service's mandatory
/// <c>TAP_SCHEMA.columns</c> description (matching each role by its IVOA UCD1+ tag) rather than
/// assumed from a hard-coded name, mirroring how <c>EsoArchiveApiClient</c> discovers real
/// products through DataLink instead of guessing a URL.
/// </summary>
public sealed class VizierTapClient : ICatalogueClient
{
    private const string SyncEndpoint = "sync";
    private const double ArcsecondsPerDegree = 3600.0;

    private static readonly string[] RightAscensionUcdAtoms = ["pos.eq.ra"];
    private static readonly string[] DeclinationUcdAtoms = ["pos.eq.dec"];
    private static readonly string[] IdentifierUcdAtoms = ["meta.id", "meta.record"];
    private static readonly string[] MagnitudeUcdAtoms = ["phot.mag"];

    private readonly HttpClient _httpClient;
    private readonly ILogger<VizierTapClient> _logger;

    public VizierTapClient(HttpClient httpClient, ILogger<VizierTapClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<CatalogueRecord>>> ConeSearchAsync(
        CatalogueConeSearchQuery query, CancellationToken cancellationToken = default)
    {
        var columnsResult = await ResolveColumnsAsync(query.CatalogueId, cancellationToken);

        if (columnsResult.IsFailure)
        {
            return Result<IReadOnlyList<CatalogueRecord>>.Failure(columnsResult.Error);
        }

        var columns = columnsResult.Value;

        var adqlQuery = BuildConeSearchAdql(query, columns);

        var tableResult = await ExecuteAdqlAsync(adqlQuery, cancellationToken);

        if (tableResult.IsFailure)
        {
            return Result<IReadOnlyList<CatalogueRecord>>.Failure(tableResult.Error);
        }

        return MapRecords(tableResult.Value);
    }

    private static string BuildConeSearchAdql(CatalogueConeSearchQuery query, ResolvedColumns columns)
    {
        var radiusDegrees = (query.RadiusArcsec / ArcsecondsPerDegree).ToString(CultureInfo.InvariantCulture);

        var rightAscension = query.RightAscension.ToString(CultureInfo.InvariantCulture);

        var declination = query.Declination.ToString(CultureInfo.InvariantCulture);

        var magnitudeSelect = columns.MagnitudeColumn is { } magnitudeColumn
            ? $"\"{EscapeAdqlIdentifier(magnitudeColumn)}\""
            : "NULL";

        var tableName = EscapeAdqlIdentifier(query.CatalogueId);

        var raColumn = EscapeAdqlIdentifier(columns.RightAscensionColumn);

        var decColumn = EscapeAdqlIdentifier(columns.DeclinationColumn);

        var idColumn = EscapeAdqlIdentifier(columns.IdentifierColumn);

        return
            $"SELECT TOP {query.MaxResults} \"{idColumn}\" AS catalogue_id, \"{raColumn}\" AS ra, \"{decColumn}\" AS dec, {magnitudeSelect} AS mag " +
            $"FROM \"{tableName}\" " +
            $"WHERE 1=CONTAINS(POINT('ICRS',\"{raColumn}\",\"{decColumn}\"),CIRCLE('ICRS',{rightAscension},{declination},{radiusDegrees}))";
    }

    private async Task<Result<ResolvedColumns>> ResolveColumnsAsync(string catalogueId, CancellationToken cancellationToken)
    {
        var adqlQuery =
            $"SELECT column_name, ucd FROM TAP_SCHEMA.columns WHERE table_name='{EscapeAdqlStringLiteral(catalogueId)}'";

        var tableResult = await ExecuteAdqlAsync(adqlQuery, cancellationToken);

        if (tableResult.IsFailure)
        {
            return Result<ResolvedColumns>.Failure(tableResult.Error);
        }

        var table = tableResult.Value;

        var schemaFieldNames = table.FieldNames.ToList();

        var nameIndex = schemaFieldNames.FindIndex(n => string.Equals(n, "column_name", StringComparison.OrdinalIgnoreCase));

        var ucdIndex = schemaFieldNames.FindIndex(n => string.Equals(n, "ucd", StringComparison.OrdinalIgnoreCase));

        if (nameIndex < 0 || ucdIndex < 0 || table.Rows.Count == 0)
        {
            return Error.NotFound(
                "catalogues.vizier.unknown_catalogue",
                $"VizieR's TAP_SCHEMA reports no columns for catalogue '{catalogueId}'; it may not exist.");
        }

        var columns = table.Rows
            .Select(row => (Name: GetCell(row, nameIndex) ?? string.Empty, Ucd: GetCell(row, ucdIndex) ?? string.Empty))
            .Where(c => c.Name.Length > 0)
            .ToList();

        var rightAscensionColumn = ResolveColumn(columns, RightAscensionUcdAtoms);

        var declinationColumn = ResolveColumn(columns, DeclinationUcdAtoms);

        if (rightAscensionColumn is null || declinationColumn is null)
        {
            return Error.NotFound(
                "catalogues.vizier.unresolved_position_columns",
                $"Could not resolve RA/Dec columns for catalogue '{catalogueId}' from its TAP_SCHEMA description.");
        }

        var identifierColumn = ResolveColumn(columns, IdentifierUcdAtoms);

        if (identifierColumn is null)
        {
            return Error.NotFound(
                "catalogues.vizier.unresolved_identifier_column",
                $"Could not resolve a source identifier column for catalogue '{catalogueId}' from its TAP_SCHEMA description.");
        }

        var magnitudeColumn = ResolveColumn(columns, MagnitudeUcdAtoms);

        return new ResolvedColumns(rightAscensionColumn, declinationColumn, identifierColumn, magnitudeColumn);
    }

    private static string? ResolveColumn(IReadOnlyList<(string Name, string Ucd)> columns, IReadOnlyList<string> ucdAtomsInPriorityOrder)
    {
        foreach (var atom in ucdAtomsInPriorityOrder)
        {
            var matches = columns.Where(c => HasUcdAtom(c.Ucd, atom)).ToList();

            if (matches.Count == 0)
            {
                continue;
            }

            var primary = matches.FirstOrDefault(c => HasUcdAtom(c.Ucd, "meta.main"));

            return primary.Name ?? matches[0].Name;
        }

        return null;
    }

    private static bool HasUcdAtom(string ucd, string atom) =>
        ucd.Split(';').Any(part => string.Equals(part.Trim(), atom, StringComparison.OrdinalIgnoreCase));

    private static List<CatalogueRecord> MapRecords(VoTableResult table)
    {
        var fieldNames = table.FieldNames.ToList();

        var idIndex = fieldNames.FindIndex(n => string.Equals(n, "catalogue_id", StringComparison.OrdinalIgnoreCase));

        var raIndex = fieldNames.FindIndex(n => string.Equals(n, "ra", StringComparison.OrdinalIgnoreCase));

        var decIndex = fieldNames.FindIndex(n => string.Equals(n, "dec", StringComparison.OrdinalIgnoreCase));

        var magIndex = fieldNames.FindIndex(n => string.Equals(n, "mag", StringComparison.OrdinalIgnoreCase));

        var records = new List<CatalogueRecord>();

        if (idIndex < 0 || raIndex < 0 || decIndex < 0)
        {
            return records;
        }

        foreach (var row in table.Rows)
        {
            var identifier = GetCell(row, idIndex);

            if (string.IsNullOrWhiteSpace(identifier))
            {
                continue;
            }

            if (!TryParseDouble(GetCell(row, raIndex), out var rightAscension) || !TryParseDouble(GetCell(row, decIndex), out var declination))
            {
                continue;
            }

            var magnitude = magIndex >= 0 && TryParseDouble(GetCell(row, magIndex), out var magnitudeValue)
                ? magnitudeValue
                : (double?)null;

            records.Add(CatalogueRecord.Create(identifier, rightAscension, declination, magnitude));
        }

        return records;
    }

    private static string? GetCell(IReadOnlyList<string?> row, int index) => index < row.Count ? row[index] : null;

    private static bool TryParseDouble(string? value, out double result) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private static string EscapeAdqlStringLiteral(string value) => value.Replace("'", "''");

    private static string EscapeAdqlIdentifier(string value) => value.Replace("\"", "\"\"");

    private async Task<Result<VoTableResult>> ExecuteAdqlAsync(string adqlQuery, CancellationToken cancellationToken)
    {
        try
        {
            var formContent = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("REQUEST", "doQuery"),
                new KeyValuePair<string, string>("LANG", "ADQL"),
                new KeyValuePair<string, string>("FORMAT", "votable"),
                new KeyValuePair<string, string>("QUERY", adqlQuery)
            ]);

            using var response = await _httpClient.PostAsync(SyncEndpoint, formContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result<VoTableResult>.Failure(await MapHttpErrorAsync(response, "catalogues.vizier", cancellationToken));
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            XDocument document;

            try
            {
                document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
            }
            catch (XmlException ex)
            {
                _logger.LogWarning(ex, "VizieR TAP response was not well-formed VOTable XML.");

                return Error.Infrastructure(
                    "catalogues.vizier.malformed_response", "VizieR returned a response that could not be parsed as VOTable XML.");
            }

            var queryErrorMessage = VoTableParser.FindQueryErrorMessage(document);

            if (queryErrorMessage is not null)
            {
                return Error.Infrastructure("catalogues.vizier.query_error", $"VizieR TAP query failed: {queryErrorMessage}");
            }

            var table = VoTableParser.Parse(document);

            if (table is null)
            {
                return Error.Infrastructure(
                    "catalogues.vizier.malformed_response", "VizieR TAP response did not contain a recognizable VOTABLE TABLE element.");
            }

            return table;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("VizieR TAP query was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during VizieR TAP query.");

            return Error.Unexpected("catalogues.vizier.query_unexpected_error", "An unexpected error occurred while querying VizieR.");
        }
    }

    private async Task<Error> MapHttpErrorAsync(HttpResponseMessage response, string errorCodePrefix, CancellationToken cancellationToken)
    {
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogWarning("VizieR API request failed with HTTP status {StatusCode}: {Error}", response.StatusCode, errorContent);

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => Error.Validation($"{errorCodePrefix}.invalid_request", "VizieR rejected the request as invalid."),
            HttpStatusCode.NotFound => Error.NotFound($"{errorCodePrefix}.not_found", "The requested VizieR resource was not found."),
            HttpStatusCode.TooManyRequests => Error.Infrastructure($"{errorCodePrefix}.rate_limited", "VizieR rate-limited this request."),
            _ when (int)response.StatusCode >= 500 => Error.Infrastructure(
                $"{errorCodePrefix}.upstream_error", $"VizieR returned an upstream error (HTTP {(int)response.StatusCode})."),
            _ => Error.Unexpected($"{errorCodePrefix}.http_error", $"VizieR API returned HTTP status {(int)response.StatusCode}."),
        };
    }

    private readonly record struct ResolvedColumns(string RightAscensionColumn, string DeclinationColumn, string IdentifierColumn, string? MagnitudeColumn);
}
