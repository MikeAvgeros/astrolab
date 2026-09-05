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

    [Fact]
    public async Task DownloadDataset_KnownArchive_StagesFileAndReturnsCreated()
    {
        const string fitsContent = "not a real FITS file, just bytes to stage";

        var mast = new StubMastArchiveClient(
            search: (_, _) => throw new InvalidOperationException("Download should not search."),
            download: (datasetId, _) =>
            {
                Assert.Equal("obs1", datasetId);

                var pipeReader = PipeReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(fitsContent)));

                var download = new ArchiveDownload("obs1.fits", fitsContent.Length, pipeReader, new HttpResponseMessage());

                return Task.FromResult(Result<ArchiveDownload>.Success(download));
            });

        var client = CreateClientWithStubArchives(NotCalledEsoClient(), mast);

        var response = await client.PostAsJsonAsync("/api/archives/download", new { Archive = "Mast", DatasetId = "obs1" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Mast", body.GetProperty("archive").GetString());
        Assert.Equal(fitsContent.Length, body.GetProperty("sizeBytes").GetInt64());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("fileId").GetString()));
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
