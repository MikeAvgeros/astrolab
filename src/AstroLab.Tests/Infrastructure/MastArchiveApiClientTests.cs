using System.Net;
using System.Text;
using AstroLab.Infrastructure.Archives;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroLab.Tests.Infrastructure;

public class MastArchiveApiClientTests
{
    private const string NameLookupResponseJson = """
        {
          "status": "SUCCESS",
          "resolvedCoordinate": [
            {"canonicalName":"MESSIER 031","ra":10.68471,"decl":41.26875}
          ]
        }
        """;

    private const string NameLookupNotFoundResponseJson = """
        {
          "status": "SUCCESS",
          "resolvedCoordinate": []
        }
        """;

    private const string MashupResponseJson = """
        {
          "status": "COMPLETE",
          "data": [
            {
              "obs_id":"obs1","target_name":"M31","obs_collection":"HST","instrument_name":"ACS/WFC",
              "dataproduct_type":"image","calib_level":3,"t_min":58000.5,"t_max":58000.6,
              "t_exptime":900.0,"s_ra":10.68,"s_dec":41.27,"em_min":0.4,"em_max":0.7,
              "proposal_id":"12345","proposal_pi":"Someone","data_rights":"PUBLIC"
            }
          ]
        }
        """;

    private const string ProductsResponseJson = """
        {
          "status": "COMPLETE",
          "data": [
            {"dataURI":"mast:HST/product/j8xi01a1q_raw.fits","productFilename":"j8xi01a1q_raw.fits","productType":"SCIENCE","dataproduct_type":"image","calib_level":1,"size":100,"dataRights":"PUBLIC"},
            {"dataURI":"mast:HST/product/j8xi01a1q_drz.fits","productFilename":"j8xi01a1q_drz.fits","productType":"SCIENCE","dataproduct_type":"image","calib_level":3,"size":200,"dataRights":"PUBLIC"}
          ]
        }
        """;

    private static (MastArchiveApiClient Client, StubHttpMessageHandler Handler) CreateClient(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://mast.test/") };
        var client = new MastArchiveApiClient(httpClient, NullLogger<MastArchiveApiClient>.Instance);
        return (client, handler);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private static async Task<string> ReadRequestJsonAsync(HttpRequestMessage request)
    {
        var body = await request.Content!.ReadAsStringAsync();
        var decoded = Uri.UnescapeDataString(body.Replace('+', ' '));
        return decoded["request=".Length..];
    }

    private static bool RequestContainsService(string requestJson, string service) =>
        requestJson.Contains($"\"service\":\"{service}\"");

    [Fact]
    public async Task ResolveTargetAsync_KnownTarget_ReturnsCoordinates()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(JsonResponse(NameLookupResponseJson)));

        var result = await client.ResolveTargetAsync("M31");

        Assert.True(result.IsSuccess);
        Assert.Equal("MESSIER 031", result.Value.Name);
        Assert.Equal(10.68471, result.Value.RightAscension);
        Assert.Equal(41.26875, result.Value.Declination);
    }

    [Fact]
    public async Task ResolveTargetAsync_UnknownTarget_ReturnsNotFoundFailure()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(JsonResponse(NameLookupNotFoundResponseJson)));

        var result = await client.ResolveTargetAsync("not-a-real-target");

        Assert.True(result.IsFailure);
        Assert.Equal("mast.target_not_resolved", result.Error.Code);
    }

    [Fact]
    public async Task ResolveTargetAsync_BlankTarget_ReturnsValidationFailure()
    {
        var (client, _) = CreateClient(_ => throw new InvalidOperationException("should not be called"));

        var result = await client.ResolveTargetAsync("   ");

        Assert.True(result.IsFailure);
        Assert.Equal("mast.invalid_target", result.Error.Code);
    }

    [Fact]
    public async Task SearchAsync_ResolvesTargetThenSearchesPositionally()
    {
        var (client, handler) = CreateClient(async request =>
        {
            var requestJson = await ReadRequestJsonAsync(request);

            return RequestContainsService(requestJson, "Mast.Name.Lookup")
                ? JsonResponse(NameLookupResponseJson)
                : JsonResponse(MashupResponseJson);
        });

        var query = ArchiveSearchQuery.Create(
            target: "M31",
            mission: "HST",
            instrument: "ACS/WFC",
            from: new DateTimeOffset(2017, 9, 1, 0, 0, 0, TimeSpan.Zero),
            to: new DateTimeOffset(2017, 9, 30, 0, 0, 0, TimeSpan.Zero),
            searchRadiusDegrees: 0.2,
            maxResults: 25);

        var result = await client.SearchAsync(query);

        Assert.True(result.IsSuccess);

        var searchRequest = handler.Requests.Last();
        var searchJson = await ReadRequestJsonAsync(searchRequest);

        Assert.Contains("\"paramName\":\"obs_collection\"", searchJson);
        Assert.Contains("\"paramName\":\"instrument_name\"", searchJson);
        Assert.Contains("\"paramName\":\"t_min\"", searchJson);
        Assert.Contains("\"min\":", searchJson);
        Assert.Contains("\"max\":", searchJson);
        Assert.Contains("\"pagesize\":25", searchJson);
        Assert.Contains("\"position\":\"10.68471, 41.26875\"", searchJson);
        Assert.Contains("\"radius\":0.2", searchJson);
        Assert.DoesNotContain("\"columns\":\"*\"", searchJson);
        Assert.Contains("\"columns\":\"obsid,obs_id,target_name", searchJson);
    }

    [Fact]
    public async Task SearchAsync_DateRange_OnlyFrom_SendsMinBoundOnly()
    {
        var (client, handler) = CreateClient(async request =>
        {
            var requestJson = await ReadRequestJsonAsync(request);

            return RequestContainsService(requestJson, "Mast.Name.Lookup")
                ? JsonResponse(NameLookupResponseJson)
                : JsonResponse(MashupResponseJson);
        });

        var query = ArchiveSearchQuery.Create(target: "M31", from: new DateTimeOffset(2017, 9, 1, 0, 0, 0, TimeSpan.Zero));

        await client.SearchAsync(query);

        var searchJson = await ReadRequestJsonAsync(handler.Requests.Last());

        Assert.Contains("\"min\":", searchJson);
        Assert.DoesNotContain("\"max\":", searchJson);
    }

    [Fact]
    public async Task SearchAsync_DateRange_OnlyTo_SendsMaxBoundOnly()
    {
        var (client, handler) = CreateClient(async request =>
        {
            var requestJson = await ReadRequestJsonAsync(request);

            return RequestContainsService(requestJson, "Mast.Name.Lookup")
                ? JsonResponse(NameLookupResponseJson)
                : JsonResponse(MashupResponseJson);
        });

        var query = ArchiveSearchQuery.Create(target: "M31", to: new DateTimeOffset(2017, 9, 30, 0, 0, 0, TimeSpan.Zero));

        await client.SearchAsync(query);

        var searchJson = await ReadRequestJsonAsync(handler.Requests.Last());

        Assert.Contains("\"max\":", searchJson);
        Assert.DoesNotContain("\"min\":", searchJson);
    }

    [Fact]
    public async Task SearchAsync_MapsRichObservationMetadata()
    {
        var (client, _) = CreateClient(async request =>
        {
            var requestJson = await ReadRequestJsonAsync(request);

            return RequestContainsService(requestJson, "Mast.Name.Lookup")
                ? JsonResponse(NameLookupResponseJson)
                : JsonResponse(MashupResponseJson);
        });

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"));

        Assert.True(result.IsSuccess);
        var observation = Assert.Single(result.Value);

        Assert.Equal("obs1", observation.DatasetId);
        Assert.Equal("M31", observation.Target);
        Assert.Equal("ACS/WFC", observation.Instrument);
        Assert.Equal(ArchiveSource.Mast, observation.Source);
        Assert.Equal("HST", observation.Collection);
        Assert.Equal("image", observation.DataProductType);
        Assert.Equal(3, observation.CalibrationLevel);
        Assert.Equal(10.68, observation.RightAscension);
        Assert.Equal(41.27, observation.Declination);
        Assert.Equal(900.0, observation.ExposureTimeSeconds);
        Assert.Equal(0.4, observation.WavelengthMinMicrometres);
        Assert.Equal(0.7, observation.WavelengthMaxMicrometres);
        Assert.Equal("12345", observation.ProposalId);
        Assert.Equal("Someone", observation.ProposalPi);
        Assert.Equal("PUBLIC", observation.DataRights);

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
    public async Task SearchAsync_FromAfterTo_ReturnsValidationFailureWithoutCallingMast()
    {
        var (client, handler) = CreateClient(_ => throw new InvalidOperationException("should not be called"));

        var query = ArchiveSearchQuery.Create(
            target: "M31",
            from: new DateTimeOffset(2017, 9, 30, 0, 0, 0, TimeSpan.Zero),
            to: new DateTimeOffset(2017, 9, 1, 0, 0, 0, TimeSpan.Zero));

        var result = await client.SearchAsync(query);

        Assert.True(result.IsFailure);
        Assert.Equal("mast.invalid_date_range", result.Error.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SearchAsync_TargetDoesNotResolve_ReturnsFailureWithoutSearching()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(JsonResponse(NameLookupNotFoundResponseJson)));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "not-a-real-target"));

        Assert.True(result.IsFailure);
        Assert.Equal("mast.target_not_resolved", result.Error.Code);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SearchAsync_NonCompleteStatus_ReturnsFailureResult()
    {
        var (client, _) = CreateClient(async request =>
        {
            var requestJson = await ReadRequestJsonAsync(request);

            return RequestContainsService(requestJson, "Mast.Name.Lookup")
                ? JsonResponse(NameLookupResponseJson)
                : JsonResponse("""{"status":"ERROR","msg":"bad query"}""");
        });

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"));

        Assert.True(result.IsFailure);
        Assert.Equal("mast.search_failed", result.Error.Code);
    }

    [Fact]
    public async Task GetProductsAsync_MapsProducts()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(JsonResponse(ProductsResponseJson)));

        var result = await client.GetProductsAsync("obs1");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("mast:HST/product/j8xi01a1q_raw.fits", result.Value[0].DataUri);

        var requestJson = await ReadRequestJsonAsync(handler.Requests.Single());
        Assert.Contains("\"service\":\"Mast.Caom.Products\"", requestJson);
        Assert.Contains("\"obsid\":\"obs1\"", requestJson);
    }

    [Fact]
    public async Task GetProductsAsync_EmptyResultSet_ReturnsSuccessWithEmptyList()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(JsonResponse("""{"status":"COMPLETE","data":[]}""")));

        var result = await client.GetProductsAsync("obs1");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetProductsAsync_BlankObservationId_ReturnsValidationFailure()
    {
        var (client, _) = CreateClient(_ => throw new InvalidOperationException("should not be called"));

        var result = await client.GetProductsAsync(string.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal("mast.invalid_observation_id", result.Error.Code);
    }
}
