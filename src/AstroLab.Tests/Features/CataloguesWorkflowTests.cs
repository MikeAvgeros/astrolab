using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.Catalogues;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AstroLab.Tests.Features;

/// <summary>
/// Covers the <c>/api/catalogues/query</c> and <c>/api/catalogues/cross-match</c> endpoints
/// end-to-end through the real API host, with <see cref="ICatalogueClient"/> replaced by a test
/// double so no real VizieR network call is made (per spec.md §7.4, API tests must not require
/// external archive services).
/// </summary>
public sealed class CataloguesWorkflowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public CataloguesWorkflowTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private HttpClient CreateClientWithStubCatalogue(StubCatalogueClient catalogueClient) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ICatalogueClient>(catalogueClient);
        })).CreateClient();

    private static StubCatalogueClient NotCalledCatalogueClient() => new(
        (_, _) => throw new InvalidOperationException("Catalogue client should not be called for this test."));

    private async Task<string> UploadAsync(byte[] fitsBytes)
    {
        using var content = new ByteArrayContent(fitsBytes);

        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var response = await _client.PostAsync("/api/fits/upload", content);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        return body.GetProperty("fileId").GetString()!;
    }

    private async Task<string> UploadImageWithSourceAndWcsAsync() => await UploadAsync(SyntheticFits.SmallImageWithSourceAndWcs());

    private async Task<string> UploadGradientImageAsync() => await UploadAsync(SyntheticFits.SmallGradientImage());

    [Fact]
    public async Task QueryCatalogue_ReturnsMappedEntries()
    {
        var record = CatalogueRecord.Create("Gaia DR3 123", rightAscension: 180.0, declination: 0.0, magnitude: 15.5);

        var catalogueClient = new StubCatalogueClient(
            (query, _) =>
            {
                Assert.Equal("I/355/gaiadr3", query.CatalogueId);
                Assert.Equal(5.0, query.RadiusArcsec);
                return Task.FromResult(Result<IReadOnlyList<CatalogueRecord>>.Success([record]));
            });

        var client = CreateClientWithStubCatalogue(catalogueClient);

        var response = await client.GetAsync("/api/catalogues/query?catalogueId=I%2F355%2Fgaiadr3&rightAscension=180&declination=0&radiusArcsec=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("I/355/gaiadr3", body.GetProperty("catalogueId").GetString());

        var entries = body.GetProperty("entries").EnumerateArray().ToArray();

        var entry = Assert.Single(entries);

        Assert.Equal("Gaia DR3 123", entry.GetProperty("identifier").GetString());
        Assert.Equal(180.0, entry.GetProperty("rightAscension").GetDouble());
        Assert.Equal(0.0, entry.GetProperty("declination").GetDouble());
        Assert.Equal(15.5, entry.GetProperty("magnitude").GetDouble());
    }

    [Fact]
    public async Task QueryCatalogue_ClientReturnsFailure_MapsToProblemResponse()
    {
        var catalogueClient = new StubCatalogueClient(
            (_, _) => Task.FromResult(Result<IReadOnlyList<CatalogueRecord>>.Failure(
                Error.NotFound("catalogues.vizier.unknown_catalogue", "Unknown catalogue."))));

        var client = CreateClientWithStubCatalogue(catalogueClient);

        var response = await client.GetAsync("/api/catalogues/query?catalogueId=not%2Fa%2Fcatalogue&rightAscension=180&declination=0&radiusArcsec=5");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("catalogues.vizier.unknown_catalogue", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task CrossMatch_CandidateNearDetectedSource_ReturnsMatch()
    {
        var fileId = await UploadImageWithSourceAndWcsAsync();

        var catalogueClient = new StubCatalogueClient(
            (query, _) =>
            {
                var candidate = CatalogueRecord.Create("Gaia DR3 999", query.RightAscension, query.Declination, magnitude: 12.3);
                return Task.FromResult(Result<IReadOnlyList<CatalogueRecord>>.Success([candidate]));
            });

        var client = CreateClientWithStubCatalogue(catalogueClient);

        var response = await client.PostAsJsonAsync(
            "/api/catalogues/cross-match",
            new { FileId = fileId, CatalogueIds = new[] { "I/355/gaiadr3" }, RadiusArcsec = 5.0 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(fileId, body.GetProperty("fileId").GetString());

        var matches = body.GetProperty("matches").EnumerateArray().ToArray();

        var match = Assert.Single(matches);

        Assert.Equal("Gaia DR3 999", match.GetProperty("catalogueIdentifier").GetString());
        Assert.Equal("I/355/gaiadr3", match.GetProperty("catalogueId").GetString());
        Assert.Equal(0.0, match.GetProperty("separationArcsec").GetDouble(), precision: 6);
    }

    [Fact]
    public async Task CrossMatch_NoCandidateWithinRadius_ReturnsNoMatches()
    {
        var fileId = await UploadImageWithSourceAndWcsAsync();

        var catalogueClient = new StubCatalogueClient(
            (query, _) =>
            {
                var farCandidate = CatalogueRecord.Create("Far Away", query.RightAscension + 10.0, query.Declination);
                return Task.FromResult(Result<IReadOnlyList<CatalogueRecord>>.Success([farCandidate]));
            });

        var client = CreateClientWithStubCatalogue(catalogueClient);

        var response = await client.PostAsJsonAsync(
            "/api/catalogues/cross-match",
            new { FileId = fileId, CatalogueIds = new[] { "I/355/gaiadr3" }, RadiusArcsec = 5.0 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Empty(body.GetProperty("matches").EnumerateArray());
    }

    [Fact]
    public async Task CrossMatch_OnImageWithoutWcs_ReturnsNotFound()
    {
        var fileId = await UploadGradientImageAsync();

        var client = CreateClientWithStubCatalogue(NotCalledCatalogueClient());

        var response = await client.PostAsJsonAsync(
            "/api/catalogues/cross-match",
            new { FileId = fileId, CatalogueIds = new[] { "I/355/gaiadr3" }, RadiusArcsec = 5.0 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CrossMatch_EmptyCatalogueIds_ReturnsBadRequest()
    {
        var fileId = await UploadImageWithSourceAndWcsAsync();

        var client = CreateClientWithStubCatalogue(NotCalledCatalogueClient());

        var response = await client.PostAsJsonAsync(
            "/api/catalogues/cross-match",
            new { FileId = fileId, CatalogueIds = Array.Empty<string>(), RadiusArcsec = 5.0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CrossMatch_CatalogueClientReturnsFailure_MapsToProblemResponse()
    {
        var fileId = await UploadImageWithSourceAndWcsAsync();

        var catalogueClient = new StubCatalogueClient(
            (_, _) => Task.FromResult(Result<IReadOnlyList<CatalogueRecord>>.Failure(
                Error.Infrastructure("catalogues.vizier.upstream_error", "VizieR is unavailable."))));

        var client = CreateClientWithStubCatalogue(catalogueClient);

        var response = await client.PostAsJsonAsync(
            "/api/catalogues/cross-match",
            new { FileId = fileId, CatalogueIds = new[] { "I/355/gaiadr3" }, RadiusArcsec = 5.0 });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("catalogues.vizier.upstream_error", body.GetProperty("title").GetString());
    }
}
