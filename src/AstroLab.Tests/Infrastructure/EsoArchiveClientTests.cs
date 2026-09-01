using System.Net;
using System.Text;
using AstroLab.Infrastructure.Archives;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroLab.Tests.Infrastructure;

public class EsoArchiveClientTests
{
    private const string TapResponseJson = """
        {
          "metadata": [
            {"name":"dp_id"},{"name":"target_name"},{"name":"obs_id"},
            {"name":"instrument_name"},{"name":"t_min"},{"name":"t_exptime"}
          ],
          "data": [
            ["ADP.123", "M31", "obs1", "FORS2", 58000.5, 300.0]
          ]
        }
        """;

    private static (EsoArchiveClient Client, StubHttpMessageHandler Handler) CreateClient(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://archive.eso.test/") };
        var client = new EsoArchiveClient(httpClient, NullLogger<EsoArchiveClient>.Instance);
        return (client, handler);
    }

    [Fact]
    public async Task SearchAsync_BuildsAdqlQuery_HonoringAllFilters()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TapResponseJson, Encoding.UTF8, "application/json")
        }));

        var query = ArchiveSearchQuery.Create(
            target: "M31",
            instrument: "FORS2",
            from: new DateTimeOffset(2017, 9, 1, 0, 0, 0, TimeSpan.Zero),
            to: new DateTimeOffset(2017, 9, 30, 0, 0, 0, TimeSpan.Zero),
            maxResults: 25);

        var result = await client.SearchAsync(query);

        Assert.True(result.IsSuccess);

        var decodedBody = Uri.UnescapeDataString(handler.LastRequestBody!.Replace('+', ' '));
        Assert.Contains("TOP 25", decodedBody);
        Assert.Contains("target_name LIKE '%M31%'", decodedBody);
        Assert.Contains("instrument_name = 'FORS2'", decodedBody);
        Assert.Contains("t_min >=", decodedBody);
        Assert.Contains("t_min <=", decodedBody);
    }

    [Fact]
    public async Task SearchAsync_MapsObservations_IncludingObservationDateFromMjd()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TapResponseJson, Encoding.UTF8, "application/json")
        }));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"));

        Assert.True(result.IsSuccess);
        var observation = Assert.Single(result.Value);

        Assert.Equal("ADP.123", observation.DatasetId);
        Assert.Equal("M31", observation.Target);
        Assert.Equal("FORS2", observation.Instrument);
        Assert.Equal(ArchiveSource.Eso, observation.Source);

        var expectedDate = new DateTimeOffset(1858, 11, 17, 0, 0, 0, TimeSpan.Zero).AddDays(58000.5);
        Assert.Equal(expectedDate, observation.ObservationDate);
    }

    [Fact]
    public async Task SearchAsync_EmptyData_ReturnsSuccessWithEmptyList()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"metadata":[],"data":[]}""", Encoding.UTF8, "application/json")
        }));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "Nothing"));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task SearchAsync_HttpFailure_ReturnsFailureResult()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("boom")
        }));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"));

        Assert.True(result.IsFailure);
        Assert.Equal("eso.search_failed", result.Error.Code);
    }

    [Fact]
    public async Task DownloadAsync_Success_StreamsContentThroughPipeReader()
    {
        var payload = "FITS-DATA"u8.ToArray();

        var (client, _) = CreateClient(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(payload)
            };
            response.Content.Headers.ContentDisposition =
                new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = "dataset.fits" };
            return Task.FromResult(response);
        });

        var result = await client.DownloadAsync("ADP.123");

        Assert.True(result.IsSuccess);
        await using var download = result.Value;

        Assert.Equal("dataset.fits", download.FileName);

        var readResult = await download.Content.ReadAsync();
        Assert.Equal(payload, System.Buffers.BuffersExtensions.ToArray(readResult.Buffer));
        download.Content.AdvanceTo(readResult.Buffer.End);
    }

    [Fact]
    public async Task DownloadAsync_NotFound_ReturnsNotFoundFailure()
    {
        var (client, _) = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var result = await client.DownloadAsync("missing-dataset");

        Assert.True(result.IsFailure);
        Assert.Equal("eso.dataset_not_found", result.Error.Code);
    }

    [Fact]
    public async Task DownloadAsync_BlankDatasetId_ReturnsValidationFailure()
    {
        var (client, _) = CreateClient(_ => throw new InvalidOperationException("should not be called"));

        var result = await client.DownloadAsync("   ");

        Assert.True(result.IsFailure);
        Assert.Equal("eso.invalid_dataset_id", result.Error.Code);
    }
}
