using AstroLab.Core.Result;
using System.Net;
using System.Text;
using AstroLab.Infrastructure.Archives;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroLab.Tests.Infrastructure;

public class EsoArchiveApiClientTests
{
    private const string TapResponseJson = """
        {
          "metadata": [
            {"name":"dp_id"},{"name":"target_name"},{"name":"obs_collection"},{"name":"instrument_name"},
            {"name":"dataproduct_type"},{"name":"calib_level"},{"name":"t_min"},{"name":"t_max"},
            {"name":"t_exptime"},{"name":"s_ra"},{"name":"s_dec"},{"name":"em_min"},{"name":"em_max"},
            {"name":"proposal_id"},{"name":"obs_creator_name"}
          ],
          "data": [
            ["ADP.123", "M31", "FORS", "FORS2", "image", 2, 58000.5, 58000.6, 300.0, 10.68, 41.27, 4e-7, 7e-7, "60.A-9203", "Someone"]
          ]
        }
        """;

    private const string DataLinkResponseJson = """
        [
          {"id":"ivo://eso.org/ID?ADP.123","access_url":"https://dataportal.eso.org/dataPortal/preview/ADP.123",
           "service_def":null,"error_message":null,"semantics":"#preview","content_type":"image/jpeg",
           "content_length":5000,"eso_origfile":null,"science":false},
          {"id":"ivo://eso.org/ID?ADP.123","access_url":"https://dataportal.eso.org/dataPortal/file/ADP.123",
           "service_def":null,"error_message":null,"semantics":"#this","content_type":"application/x-fits",
           "content_length":200000,"eso_origfile":"FORS2.2017-09-01T01:02:03.000.fits","science":true},
          {"id":"ivo://eso.org/ID?ADP.123","access_url":null,"service_def":"ADP.123_soda","error_message":null,
           "semantics":"#cutout","content_type":"application/x-fits","content_length":null,"eso_origfile":null,"science":false}
        ]
        """;

    private static (EsoArchiveApiClient Client, StubHttpMessageHandler Handler) CreateClient(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://archive.eso.test/") };
        var client = new EsoArchiveApiClient(httpClient, NullLogger<EsoArchiveApiClient>.Instance);
        return (client, handler);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task SearchAsync_BuildsAdqlQuery_HonoringAllFilters()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(JsonResponse(TapResponseJson)));

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
        Assert.Contains("t_max >=", decodedBody);
        Assert.Contains("t_min <=", decodedBody);
        Assert.DoesNotContain("SELECT *", decodedBody);
        Assert.Contains("SELECT TOP 25 dp_id,target_name", decodedBody);
    }

    [Fact]
    public async Task SearchAsync_TargetFilter_UsesLikeWithoutUnsupportedEscapeClause()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(JsonResponse(TapResponseJson)));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "NGC_1234"));

        Assert.True(result.IsSuccess);

        var decodedBody = Uri.UnescapeDataString(handler.LastRequestBody!.Replace('+', ' '));

        Assert.Contains("target_name LIKE '%NGC_1234%'", decodedBody);
        Assert.DoesNotContain("ESCAPE", decodedBody);
    }

    [Fact]
    public async Task SearchAsync_TargetContainingQuote_EscapesTheAdqlStringLiteral()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(JsonResponse(TapResponseJson)));

        await client.SearchAsync(ArchiveSearchQuery.Create(target: "Barnard's Star"));

        var decodedBody = Uri.UnescapeDataString(handler.LastRequestBody!.Replace('+', ' '));

        Assert.Contains("target_name LIKE '%Barnard''s Star%'", decodedBody);
    }

    [Fact]
    public async Task SearchAsync_RequestsOnlyColumnsPresentInEsoObsCore()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(JsonResponse(TapResponseJson)));

        await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"));

        var decodedBody = Uri.UnescapeDataString(handler.LastRequestBody!.Replace('+', ' '));

        Assert.DoesNotContain("data_rights", decodedBody);
    }

    [Fact]
    public async Task SearchAsync_NoDates_AddsNoTemporalPredicate()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(JsonResponse(TapResponseJson)));

        await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"));

        var decodedBody = Uri.UnescapeDataString(handler.LastRequestBody!.Replace('+', ' '));
        Assert.DoesNotContain("t_max >=", decodedBody);
        Assert.DoesNotContain("t_min <=", decodedBody);
    }

    [Fact]
    public async Task SearchAsync_FromOnly_UsesOverlapSemantics()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(JsonResponse(TapResponseJson)));

        await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31", from: new DateTimeOffset(2017, 9, 1, 0, 0, 0, TimeSpan.Zero)));

        var decodedBody = Uri.UnescapeDataString(handler.LastRequestBody!.Replace('+', ' '));
        Assert.Contains("t_max >=", decodedBody);
        Assert.DoesNotContain("t_min <=", decodedBody);
    }

    [Fact]
    public async Task SearchAsync_ToOnly_UsesOverlapSemantics()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(JsonResponse(TapResponseJson)));

        await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31", to: new DateTimeOffset(2017, 9, 30, 0, 0, 0, TimeSpan.Zero)));

        var decodedBody = Uri.UnescapeDataString(handler.LastRequestBody!.Replace('+', ' '));
        Assert.Contains("t_min <=", decodedBody);
        Assert.DoesNotContain("t_max >=", decodedBody);
    }

    [Fact]
    public async Task SearchAsync_MapsRichObservationMetadata()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(JsonResponse(TapResponseJson)));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"));

        Assert.True(result.IsSuccess);
        var observation = Assert.Single(result.Value);

        Assert.Equal("ADP.123", observation.DatasetId);
        Assert.Equal("M31", observation.Target);
        Assert.Equal("FORS2", observation.Instrument);
        Assert.Equal(ArchiveSource.Eso, observation.Source);
        Assert.Equal("FORS", observation.Collection);
        Assert.Equal("image", observation.DataProductType);
        Assert.Equal(2, observation.CalibrationLevel);
        Assert.Equal(10.68, observation.RightAscension);
        Assert.Equal(41.27, observation.Declination);
        Assert.Equal(300.0, observation.ExposureTimeSeconds);
        Assert.Equal(0.4, observation.WavelengthMinMicrometres!.Value, precision: 9);
        Assert.Equal(0.7, observation.WavelengthMaxMicrometres!.Value, precision: 9);
        Assert.Equal("60.A-9203", observation.ProposalId);
        Assert.Equal("Someone", observation.ProposalPi);
        Assert.Null(observation.DataRights);

        var expectedDate = new DateTimeOffset(1858, 11, 17, 0, 0, 0, TimeSpan.Zero).AddDays(58000.5);
        Assert.Equal(expectedDate, observation.ObservationDate);
    }

    [Fact]
    public async Task SearchAsync_MissingOptionalColumns_MapsNullsGracefully()
    {
        const string minimalJson = """
            {
              "metadata": [{"name":"dp_id"},{"name":"target_name"},{"name":"instrument_name"}],
              "data": [["ADP.999", "M42", null]]
            }
            """;

        var (client, _) = CreateClient(_ => Task.FromResult(JsonResponse(minimalJson)));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M42"));

        Assert.True(result.IsSuccess);
        var observation = Assert.Single(result.Value);

        Assert.Equal("UNKNOWN", observation.Instrument);
        Assert.Null(observation.Collection);
        Assert.Null(observation.RightAscension);
        Assert.Null(observation.CalibrationLevel);
    }

    [Fact]
    public async Task SearchAsync_MissingDatasetId_SkipsRow()
    {
        const string json = """
            {
              "metadata": [{"name":"dp_id"},{"name":"target_name"},{"name":"instrument_name"}],
              "data": [[null, "M42", "FORS2"]]
            }
            """;

        var (client, _) = CreateClient(_ => Task.FromResult(JsonResponse(json)));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M42"));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task SearchAsync_EmptyData_ReturnsSuccessWithEmptyList()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(JsonResponse("""{"metadata":[],"data":[]}""")));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "Nothing"));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task SearchAsync_WithSearchRadius_ReturnsNotImplementedWithoutSendingARequest()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(JsonResponse(TapResponseJson)));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31", searchRadiusDegrees: 0.5));

        Assert.True(result.IsFailure);

        Assert.Equal("eso.radius_search_not_supported", result.Error.Code);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SearchAsync_NetworkFailure_ReturnsUnavailableInfrastructureFailure()
    {
        var (client, _) = CreateClient(_ => throw new HttpRequestException("connection refused"));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"));

        Assert.True(result.IsFailure);
        Assert.Equal("eso.search_unavailable", result.Error.Code);
        Assert.Equal(ErrorCategory.Infrastructure, result.Error.Category);
    }

    [Fact]
    public async Task SearchAsync_HttpClientTimeout_ReturnsUnavailableInfrastructureFailure()
    {
        var (client, _) = CreateClient(_ => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout."));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("eso.search_unavailable", result.Error.Code);
    }

    [Fact]
    public async Task SearchAsync_MalformedResponseBodyWithHttp200_ReturnsFailureRatherThanEmptySuccess()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(JsonResponse("""{"QUERY_STATUS":"ERROR","message":"unknown table"}""")));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"));

        Assert.True(result.IsFailure);
        Assert.Equal("eso.search_malformed_response", result.Error.Code);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "eso.search.invalid_request")]
    [InlineData(HttpStatusCode.Unauthorized, "eso.search.unauthorized")]
    [InlineData(HttpStatusCode.NotFound, "eso.search.not_found")]
    [InlineData(HttpStatusCode.TooManyRequests, "eso.search.rate_limited")]
    [InlineData(HttpStatusCode.InternalServerError, "eso.search.upstream_error")]
    public async Task SearchAsync_MapsHttpStatusToDistinctErrorCodes(HttpStatusCode statusCode, string expectedCode)
    {
        var (client, _) = CreateClient(_ => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("boom")
        }));

        var result = await client.SearchAsync(ArchiveSearchQuery.Create(target: "M31"));

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task GetProductsAsync_MapsProducts_SkippingRowsWithoutAccessUrl()
    {
        var (client, handler) = CreateClient(_ => Task.FromResult(JsonResponse(DataLinkResponseJson)));

        var result = await client.GetProductsAsync("ADP.123");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("https://dataportal.eso.org/dataPortal/file/ADP.123", result.Value[1].DataUri);
        Assert.Equal("#this", result.Value[1].ProductType);
        Assert.Equal("application/x-fits", result.Value[1].Format);
        Assert.Equal(200000, result.Value[1].Size);
        Assert.Equal("FORS2.2017-09-01T01:02:03.000.fits", result.Value[1].FileName);
        Assert.Null(result.Value[0].FileName);
        
        var decodedRequestUri = Uri.UnescapeDataString(handler.LastRequest!.RequestUri!.PathAndQuery);
        Assert.Contains("ID=ivo://eso.org/ID?ADP.123", decodedRequestUri);
        Assert.Contains("RESPONSEFORMAT=json", decodedRequestUri);
    }

    [Fact]
    public async Task GetProductsAsync_RowWithErrorMessage_IsSkipped()
    {
        const string json = """
            [{"id":"broken","access_url":"https://dataportal.eso.org/x","error_message":"not available"}]
            """;

        var (client, _) = CreateClient(_ => Task.FromResult(JsonResponse(json)));

        var result = await client.GetProductsAsync("ADP.123");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetProductsAsync_EmptyResultSet_ReturnsSuccessWithEmptyList()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(JsonResponse("[]")));

        var result = await client.GetProductsAsync("ADP.123");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetProductsAsync_MalformedResponseBodyWithHttp200_ReturnsFailureRatherThanEmptySuccess()
    {
        var (client, _) = CreateClient(_ => Task.FromResult(JsonResponse("""{"QUERY_STATUS":"ERROR","message":"unknown dataset"}""")));

        var result = await client.GetProductsAsync("ADP.123");

        Assert.True(result.IsFailure);
        Assert.Equal("eso.products_malformed_response", result.Error.Code);
    }

    [Fact]
    public async Task GetProductsAsync_BlankDatasetId_ReturnsValidationFailure()
    {
        var (client, _) = CreateClient(_ => throw new InvalidOperationException("should not be called"));

        var result = await client.GetProductsAsync(string.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal("eso.invalid_dataset_id", result.Error.Code);
    }
}
