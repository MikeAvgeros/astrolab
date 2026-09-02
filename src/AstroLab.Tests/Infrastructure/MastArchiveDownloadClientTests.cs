using System.Net;
using AstroLab.Infrastructure.Archives;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroLab.Tests.Infrastructure;

public class MastArchiveDownloadClientTests
{
    private static (MastArchiveDownloadClient Client, StubHttpMessageHandler Handler) CreateClient(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://mast.test/") };
        var client = new MastArchiveDownloadClient(httpClient, NullLogger<MastArchiveDownloadClient>.Instance);
        return (client, handler);
    }

    [Fact]
    public async Task DownloadAsync_UsesDataUriDirectly()
    {
        var (client, handler) = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent("FITS-DATA"u8.ToArray()) }));

        var product = new MastProduct("mast:JWST/product/custom.fits", "custom.fits", "SCIENCE", "image", 3, 123, "PUBLIC");

        var result = await client.DownloadAsync(product);

        Assert.True(result.IsSuccess);
        await using var download = result.Value;

        Assert.Contains("mast%3AJWST%2Fproduct%2Fcustom.fits", handler.Requests.Single().RequestUri!.ToString());
    }

    [Fact]
    public async Task DownloadAsync_BlankDataUri_ReturnsValidationFailure()
    {
        var (client, _) = CreateClient(_ => throw new InvalidOperationException("should not be called"));

        var product = new MastProduct("   ", "custom.fits", "SCIENCE", "image", 3, 123, "PUBLIC");

        var result = await client.DownloadAsync(product);

        Assert.True(result.IsFailure);
        Assert.Equal("mast.invalid_data_uri", result.Error.Code);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "mast.download.invalid_request")]
    [InlineData(HttpStatusCode.Unauthorized, "mast.download.unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "mast.download.forbidden")]
    [InlineData(HttpStatusCode.NotFound, "mast.download.not_found")]
    [InlineData(HttpStatusCode.TooManyRequests, "mast.download.rate_limited")]
    [InlineData(HttpStatusCode.InternalServerError, "mast.download.upstream_error")]
    public async Task DownloadAsync_MapsHttpStatusToDistinctErrorCodes(HttpStatusCode statusCode, string expectedCode)
    {
        var (client, _) = CreateClient(_ => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("error detail")
        }));

        var product = new MastProduct("mast:JWST/product/custom.fits", "custom.fits", "SCIENCE", "image", 3, 123, "PUBLIC");

        var result = await client.DownloadAsync(product);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }
}
