using System.IO.Pipelines;
using System.Net;
using System.Net.Http.Headers;
using AstroLab.Core.Result;
using Microsoft.Extensions.Logging;

namespace AstroLab.Infrastructure.Archives;

public sealed class EsoArchiveDownloadClient : IEsoArchiveDownloadClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EsoArchiveDownloadClient> _logger;

    public EsoArchiveDownloadClient(HttpClient httpClient, ILogger<EsoArchiveDownloadClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<ArchiveDownload>> DownloadAsync(EsoProduct product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (string.IsNullOrWhiteSpace(product.DataUri))
        {
            return Result<ArchiveDownload>.Failure(
                Error.Validation("eso.invalid_data_uri", "The product's access URI must not be empty."));
        }

        try
        {
            var response = await _httpClient.GetAsync(product.DataUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await MapHttpErrorAsync(response, "eso.download", cancellationToken);

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
            _logger.LogWarning("ESO download was canceled for product {ProductId}", product.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while downloading ESO product {ProductId}", product.Id);
            return Result<ArchiveDownload>.Failure(Error.Unexpected("eso.download_exception", ex.Message));
        }
    }

    private static string ExtractFileName(HttpContentHeaders headers, EsoProduct product)
    {
        if (headers.ContentDisposition?.FileName is { } fileName)
        {
            return fileName.Trim('"');
        }

        if (!string.IsNullOrWhiteSpace(product.FileName))
        {
            return product.FileName;
        }

        var derivedName = Uri.TryCreate(product.DataUri, UriKind.Absolute, out var uri)
            ? Path.GetFileName(uri.AbsolutePath)
            : null;

        return string.IsNullOrEmpty(derivedName) ? $"{product.Id}.fits" : derivedName;
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
