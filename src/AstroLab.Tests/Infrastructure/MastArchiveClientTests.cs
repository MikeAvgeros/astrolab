using System.Net;
using System.Text;
using AstroLab.Infrastructure.Archives;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroLab.Tests.Infrastructure;

public class MastArchiveClientTests
{
    private const string MashupResponseJson = """
        {
          "status": "COMPLETE",
          "data": [
            {"obs_id":"obs1","target_name":"M31","instrument_name":"ACS/WFC","t_min":58000.5}
          ]
        }
        """;

    private static (MastArchiveClient Client, StubHttpMessageHandler Handler) CreateClient(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://mast.test/") };
        var client = new MastArchiveClient(httpClient, NullLogger<MastArchiveClient>.Instance);
        return (client, handler);
    }

    private static string DecodeRequestJson(StubHttpMessageHandler handler)
    {
        var decoded = Uri.UnescapeDataString(handler.LastRequestBody!.Replace('+', ' '));
        return decoded["request=".Length..];
    }

    [Fact]
    public async Task SearchAsync_BuildsMashupFilters_HonoringAllFilters()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(MashupResponseJson, Encoding.UTF8, "application/json")
        }));

        var query = ArchiveSearchQuery.Create(
            target: "M31",
            instrument: "ACS/WFC",
            from: new DateTimeOffset(2017, 9, 1, 0, 0, 0, TimeSpan.Zero),
            to: new DateTimeOffset(2017, 9, 30, 0, 0, 0, TimeSpan.Zero),
            maxResults: 25);

        var result = await client.SearchAsync(query);

        Assert.True(result.IsSuccess);

        var requestJson = DecodeRequestJson(handler);
        Assert.Contains("\"paramName\":\"target_name\"", requestJson);
        Assert.Contains("\"paramName\":\"instrument_name\"", requestJson);
        Assert.Contains("\"paramName\":\"t_min\"", requestJson);
        Assert.Contains("\"min\":", requestJson);
        Assert.Contains("\"max\":", requestJson);
        Assert.Contains("\"pagesize\":25", requestJson);
    }

    [Fact]
    public async Task SearchAsync_MapsObservations_IncludingObservationDateFromMjd()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(MashupResponseJson, Encoding.UTF8, "application/json")
        }));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"));

        Assert.True(result.IsSuccess);
        var observation = Assert.Single(result.Value);

        Assert.Equal("obs1", observation.DatasetId);
        Assert.Equal("M31", observation.Target);
        Assert.Equal("ACS/WFC", observation.Instrument);
        Assert.Equal(ArchiveSource.Mast, observation.Source);

        var expectedDate = new DateTimeOffset(1858, 11, 17, 0, 0, 0, TimeSpan.Zero).AddDays(58000.5);
        Assert.Equal(expectedDate, observation.ObservationDate);
    }

    [Fact]
    public async Task SearchAsync_BlankTarget_ReturnsValidationFailure()
    {
        var (client, _) = CreateClient(_ => throw new InvalidOperationException("should not be called"));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "   "));

        Assert.True(result.IsFailure);
        Assert.Equal("mast.invalid_target", result.Error.Code);
    }

    [Fact]
    public async Task SearchAsync_NonCompleteStatus_ReturnsFailureResult()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"status":"ERROR","msg":"bad query"}""", Encoding.UTF8, "application/json")
        }));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"));

        Assert.True(result.IsFailure);
        Assert.Equal("mast.search_failed", result.Error.Code);
    }

    [Fact]
    public async Task DownloadAsync_NonMastPrefixedId_UsesDefaultProductTemplate()
    {
        var (client, handler) = CreateClient(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent("FITS-DATA"u8.ToArray())
            };
            return Task.FromResult(response);
        });

        var result = await client.DownloadAsync("j8xi01a1q");

        Assert.True(result.IsSuccess);
        await using var download = result.Value;

        Assert.Contains("mast%3AHST%2Fproduct%2Fj8xi01a1q%2Fj8xi01a1q_raw.fits", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task DownloadAsync_MastPrefixedId_PassesThroughUnmodified()
    {
        var (client, handler) = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent("FITS-DATA"u8.ToArray())
            }));

        var result = await client.DownloadAsync("mast:JWST/product/custom.fits");

        Assert.True(result.IsSuccess);
        await using var download = result.Value;

        Assert.Contains("mast%3AJWST%2Fproduct%2Fcustom.fits", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task DownloadAsync_NotFound_ReturnsNotFoundFailure()
    {
        var (client, _) = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var result = await client.DownloadAsync("missing-dataset");

        Assert.True(result.IsFailure);
        Assert.Equal("mast.dataset_not_found", result.Error.Code);
    }

    [Fact]
    public async Task DownloadAsync_BlankDatasetId_ReturnsValidationFailure()
    {
        var (client, _) = CreateClient(_ => throw new InvalidOperationException("should not be called"));

        var result = await client.DownloadAsync(string.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal("mast.invalid_dataset_id", result.Error.Code);
    }
}
