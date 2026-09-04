using System.Net;
using AstroLab.Infrastructure.Archives;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroLab.Tests.Infrastructure;

public class EsoArchiveDownloadClientTests
{
    private static (EsoArchiveDownloadClient Client, StubHttpMessageHandler Handler) CreateClient(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://archive.eso.test/") };
        var client = new EsoArchiveDownloadClient(httpClient, NullLogger<EsoArchiveDownloadClient>.Instance);
        return (client, handler);
    }

    [Fact]
    public async Task DownloadAsync_UsesDataUriDirectly_AndExtractsFilename()
    {
        var payload = "FITS-DATA"u8.ToArray();

        var (client, handler) = CreateClient(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) };
            response.Content.Headers.ContentDisposition =
                new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = "dataset.fits" };
            return Task.FromResult(response);
        });

        var product = EsoProduct.Create("ADP.123-this", "ADP.123", null, "https://dataportal.eso.org/dataPortal/file/ADP.123", "#this", null, 2, "application/x-fits", 100, "PUBLIC");

        var result = await client.DownloadAsync(product);

        Assert.True(result.IsSuccess);
        await using var download = result.Value;

        Assert.Equal("dataset.fits", download.FileName);
        Assert.Equal("https://dataportal.eso.org/dataPortal/file/ADP.123", handler.Requests.Single().RequestUri!.ToString());

        var readResult = await download.Content.ReadAsync();
        Assert.Equal(payload, System.Buffers.BuffersExtensions.ToArray(readResult.Buffer));
        download.Content.AdvanceTo(readResult.Buffer.End);
    }

    [Fact]
    public async Task DownloadAsync_NoContentDisposition_FallsBackToUriSegment()
    {
        var (client, _) = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent("FITS-DATA"u8.ToArray()) }));

        var product = EsoProduct.Create("ADP.123-this", "ADP.123", null, "https://dataportal.eso.org/dataPortal/file/ADP.123", "#this", null, null, null, null, null);

        var result = await client.DownloadAsync(product);

        Assert.True(result.IsSuccess);
        await using var download = result.Value;

        Assert.Equal("ADP.123", download.FileName);
    }

    [Fact]
    public async Task DownloadAsync_BlankDataUri_ReturnsValidationFailure()
    {
        var (client, _) = CreateClient(_ => throw new InvalidOperationException("should not be called"));

        var product = EsoProduct.Create("ADP.123-this", "ADP.123", null, "   ", "#this", null, null, null, null, null);

        var result = await client.DownloadAsync(product);

        Assert.True(result.IsFailure);
        Assert.Equal("eso.invalid_data_uri", result.Error.Code);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "eso.download.invalid_request")]
    [InlineData(HttpStatusCode.Unauthorized, "eso.download.unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "eso.download.forbidden")]
    [InlineData(HttpStatusCode.NotFound, "eso.download.not_found")]
    [InlineData(HttpStatusCode.TooManyRequests, "eso.download.rate_limited")]
    [InlineData(HttpStatusCode.InternalServerError, "eso.download.upstream_error")]
    public async Task DownloadAsync_MapsHttpStatusToDistinctErrorCodes(HttpStatusCode statusCode, string expectedCode)
    {
        var (client, _) = CreateClient(_ => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("error detail")
        }));

        var product = EsoProduct.Create("ADP.123-this", "ADP.123", null, "https://dataportal.eso.org/dataPortal/file/ADP.123", "#this", null, null, null, null, null);

        var result = await client.DownloadAsync(product);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }
}
