using System.IO.Pipelines;
using System.Net;
using System.Net.Http.Headers;
using AstroLab.Core.Result;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.Archives;

/// <summary>
/// Streams a FITS product's bytes from MAST via <see cref="System.IO.Pipelines.PipeReader"/> with
/// <see cref="HttpCompletionOption.ResponseHeadersRead"/>. Registered with
/// <see cref="Timeout.InfiniteTimeSpan"/> and no resilience handler, since a large download must
/// never be auto-retried and is governed solely by the caller's cancellation token.
/// </summary>
public sealed class MastArchiveDownloadClient : IMastArchiveDownloadClient
{
    private const string DownloadEndpoint = "api/v0/download/file";

    private readonly HttpClient _httpClient;
    private readonly ILogger<MastArchiveDownloadClient> _logger;

    public MastArchiveDownloadClient(HttpClient httpClient, ILogger<MastArchiveDownloadClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ArchiveDownload>> DownloadAsync(MastProduct product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (string.IsNullOrWhiteSpace(product.DataUri))
        {
            return Result<ArchiveDownload>.Failure(
                Error.Validation("mast.invalid_data_uri", "The product's dataUri must not be empty."));
        }

        var requestUri = $"{DownloadEndpoint}?uri={Uri.EscapeDataString(product.DataUri)}";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await MapHttpErrorAsync(response, "mast.download", cancellationToken);

                response.Dispose();

                return Result<ArchiveDownload>.Failure(error);
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var pipeReader = PipeReader.Create(responseStream);

            var fileName = ExtractFileName(response.Content.Headers, product);

            var contentLength = response.Content.Headers.ContentLength;

            return Result<ArchiveDownload>.Success(new ArchiveDownload(fileName, contentLength, pipeReader, response));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Download canceled for MAST product {DataUri}", product.DataUri);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during download for MAST product {DataUri}", product.DataUri);
            return Result<ArchiveDownload>.Failure(
                Error.Unexpected("mast.download_unexpected_error", "An unexpected error occurred while downloading the product."));
        }
    }

    private static string ExtractFileName(HttpContentHeaders headers, MastProduct product)
    {
        if (headers.ContentDisposition?.FileName is { } fileName)
        {
            return fileName.Trim('"');
        }

        if (!string.IsNullOrWhiteSpace(product.Filename))
        {
            return product.Filename;
        }

        var derivedName = Path.GetFileName(product.DataUri);

        return string.IsNullOrEmpty(derivedName) ? "download.fits" : derivedName;
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
