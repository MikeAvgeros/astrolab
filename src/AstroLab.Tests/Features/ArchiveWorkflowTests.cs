using System.IO.Pipelines;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AstroLab.Core.Result;
using AstroLab.Infrastructure.Archives;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AstroLab.Tests.Features;

/// <summary>
/// Covers the <c>/api/archives/search</c> and <c>/api/archives/download</c> endpoints' happy paths
/// end-to-end through the real API host, with <see cref="IEsoArchiveClient"/>/
/// <see cref="IMastArchiveClient"/> replaced by test doubles so no real ESO/MAST network call is
/// made (per spec.md §7.4, API tests must not require external archive services).
/// </summary>
public sealed class ArchiveWorkflowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ArchiveWorkflowTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithStubArchives(StubEsoArchiveClient eso, StubMastArchiveClient mast) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IEsoArchiveClient>(eso);
            services.AddSingleton<IMastArchiveClient>(mast);
        })).CreateClient();

    private static StubEsoArchiveClient NotCalledEsoClient() => new(
        search: (_, _) => throw new InvalidOperationException("ESO client should not be called for this test."),
        download: (_, _) => throw new InvalidOperationException("ESO client should not be called for this test."));

    private static StubMastArchiveClient NotCalledMastClient() => new(
        search: (_, _) => throw new InvalidOperationException("MAST client should not be called for this test."),
        download: (_, _) => throw new InvalidOperationException("MAST client should not be called for this test."));

    [Fact]
    public async Task SearchObservations_KnownArchive_ReturnsMappedObservations()
    {
        var observation = ArchiveObservation.Create(
            "obs1", "M31", "ACS/WFC", new DateTimeOffset(2017, 9, 1, 0, 0, 0, TimeSpan.Zero), ArchiveSource.Eso,
            collection: "HST");

        var eso = new StubEsoArchiveClient(
            search: (_, _) => Task.FromResult(Result<IReadOnlyList<ArchiveObservation>>.Success([observation])),
            download: (_, _) => throw new InvalidOperationException("Search should not download."));

        var client = CreateClientWithStubArchives(eso, NotCalledMastClient());

        var response = await client.GetAsync("/api/archives/search?archive=Eso&target=M31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var observations = body.GetProperty("observations");

        Assert.Equal(1, observations.GetArrayLength());

        var first = observations[0];

        Assert.Equal("obs1", first.GetProperty("datasetId").GetString());
        Assert.Equal("M31", first.GetProperty("target").GetString());
        Assert.Equal("ACS/WFC", first.GetProperty("instrument").GetString());
        Assert.Equal("HST", first.GetProperty("collection").GetString());
    }

    [Fact]
    public async Task SearchObservations_WithFromAndToQueryParameters_ForwardsDateRangeToArchiveQuery()
    {
        var observation = ArchiveObservation.Create(
            "obs1", "M31", "ACS/WFC", new DateTimeOffset(2017, 9, 1, 0, 0, 0, TimeSpan.Zero), ArchiveSource.Eso);

        ArchiveSearchQuery? capturedQuery = null;

        var eso = new StubEsoArchiveClient(
            search: (query, _) =>
            {
                capturedQuery = query;
                return Task.FromResult(Result<IReadOnlyList<ArchiveObservation>>.Success([observation]));
            },
            download: (_, _) => throw new InvalidOperationException("Search should not download."));

        var client = CreateClientWithStubArchives(eso, NotCalledMastClient());

        var response = await client.GetAsync(
            "/api/archives/search?archive=Eso&target=M31&from=2020-01-01T00:00:00Z&to=2020-12-31T00:00:00Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.NotNull(capturedQuery);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), capturedQuery.Value.From);
        Assert.Equal(new DateTimeOffset(2020, 12, 31, 0, 0, 0, TimeSpan.Zero), capturedQuery.Value.To);
    }

    [Fact]
    public async Task SearchObservations_ArchiveReturnsFailure_MapsToProblemResponse()
    {
        var eso = new StubEsoArchiveClient(
            search: (_, _) => Task.FromResult(Result<IReadOnlyList<ArchiveObservation>>.Failure(
                Error.Validation("eso.invalid_target", "Target name must be provided."))),
            download: (_, _) => throw new InvalidOperationException("Search should not download."));

        var client = CreateClientWithStubArchives(eso, NotCalledMastClient());

        var response = await client.GetAsync("/api/archives/search?archive=Eso&target=M31");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("eso.invalid_target", body.GetProperty("title").GetString());
    }

    private static StubMastArchiveClient MastClientDownloading(byte[] content) => new(
        search: (_, _) => throw new InvalidOperationException("Download should not search."),
        download: (datasetId, _) =>
        {
            Assert.Equal("obs1", datasetId);

            var pipeReader = PipeReader.Create(new MemoryStream(content));

            var download = new ArchiveDownload("obs1.fits", content.Length, pipeReader, new HttpResponseMessage());

            return Task.FromResult(Result<ArchiveDownload>.Success(download));
        });

    private string[] StagedFiles() =>
        Directory.Exists(_factory.StorageRoot) ? Directory.GetFiles(_factory.StorageRoot, "*", SearchOption.AllDirectories) : [];

    [Fact]
    public async Task DownloadDataset_KnownArchive_StagesFileAndReturnsCreated()
    {
        var fitsContent = SyntheticFits.SmallGradientImage();

        var client = CreateClientWithStubArchives(NotCalledEsoClient(), MastClientDownloading(fitsContent));

        var response = await client.PostAsJsonAsync("/api/archives/download", new { Archive = "Mast", DatasetId = "obs1" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Mast", body.GetProperty("archive").GetString());
        Assert.Equal(fitsContent.Length, body.GetProperty("sizeBytes").GetInt64());

        var fileId = body.GetProperty("fileId").GetString()!;

        Assert.Equal(fitsContent, await File.ReadAllBytesAsync(Path.Combine(_factory.StorageRoot, fileId)));
    }

    [Fact]
    public async Task DownloadDataset_ArchiveReturnsNonFitsContent_ReturnsBadGatewayAndDiscardsStagedFile()
    {
        // e.g. an archive answering 200 OK with an HTML error or login page instead of the dataset.
        var htmlErrorPage = Encoding.UTF8.GetBytes("<!DOCTYPE html><html><body>Service temporarily unavailable</body></html>");

        var client = CreateClientWithStubArchives(NotCalledEsoClient(), MastClientDownloading(htmlErrorPage));

        var stagedBefore = StagedFiles().Length;

        var response = await client.PostAsJsonAsync("/api/archives/download", new { Archive = "Mast", DatasetId = "obs1" });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("archive.download_invalid_fits", body.GetProperty("title").GetString());
        Assert.Contains("fits.header.truncated_file", body.GetProperty("detail").GetString());

        Assert.Equal(stagedBefore, StagedFiles().Length);
    }

    [Fact]
    public async Task DownloadDataset_OmittedArchiveField_ReturnsBadRequest_InsteadOfDefaultingToEso()
    {
        var client = CreateClientWithStubArchives(NotCalledEsoClient(), NotCalledMastClient());

        var response = await client.PostAsJsonAsync("/api/archives/download", new { DatasetId = "obs1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DownloadDataset_ArchiveReturnsFailure_MapsToProblemResponse()
    {
        var eso = new StubEsoArchiveClient(
            search: (_, _) => throw new InvalidOperationException("Download should not search."),
            download: (_, _) => Task.FromResult(Result<ArchiveDownload>.Failure(
                Error.NotFound("eso.dataset_not_found", "The requested dataset was not found."))));

        var client = CreateClientWithStubArchives(eso, NotCalledMastClient());

        var response = await client.PostAsJsonAsync("/api/archives/download", new { Archive = "Eso", DatasetId = "missing" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("eso.dataset_not_found", body.GetProperty("title").GetString());
    }
}
