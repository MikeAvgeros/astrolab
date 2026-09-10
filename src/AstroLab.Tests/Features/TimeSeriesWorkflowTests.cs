using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AstroLab.Tests.Infrastructure;

namespace AstroLab.Tests.Features;

/// <summary>
/// End-to-end integration tests for the light-curve endpoint: upload -> CFITSIO-backed table read
/// -> response mapping. Unlike the rest of <see cref="FitsWorkflowTests"/>, this exercises the
/// only reader in this codebase that goes through the native cfitsio library, so every test
/// dynamically skips (see <see cref="CfitsIoNativeAvailability"/>) on a machine without it.
/// </summary>
public class TimeSeriesWorkflowTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public TimeSeriesWorkflowTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLightCurve_ValidTimeSeriesTable_ReturnsTimeAndFluxColumns()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        double[] time = [0.0, 1.5, 3.0];
        double[] flux = [10.0, 20.0, 30.0];

        var fileId = await UploadAsync(SyntheticFits.TimeSeriesBinaryTable(time, flux));

        var response = await _client.GetAsync($"/api/timeseries/{fileId}/light-curve");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var returnedTime = body.GetProperty("time").EnumerateArray().Select(e => e.GetDouble()).ToArray();

        var returnedFlux = body.GetProperty("flux").EnumerateArray().Select(e => e.GetDouble()).ToArray();

        Assert.Equal(time, returnedTime);
        Assert.Equal(flux, returnedFlux);
    }

    [Fact]
    public async Task GetLightCurve_ImageFile_ReturnsBadRequest()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        var fileId = await UploadAsync(SyntheticFits.SmallGradientImage());

        var response = await _client.GetAsync($"/api/timeseries/{fileId}/light-curve");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("fits.data.unsupported_type", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Detrend_Linear_RemovesPerfectLinearTrendExactly()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        double[] time = [0.0, 1.0, 2.0, 3.0, 4.0];

        double[] flux = [1.0, 3.0, 5.0, 7.0, 9.0];

        var fileId = await UploadAsync(SyntheticFits.TimeSeriesBinaryTable(time, flux));

        var response = await _client.PostAsJsonAsync($"/api/timeseries/{fileId}/detrend", new { Method = "linear" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var detrendedFlux = body.GetProperty("detrendedFlux").EnumerateArray().Select(e => e.GetDouble()).ToArray();

        Assert.All(detrendedFlux, value => Assert.Equal(0.0, value, precision: 6));
    }

    [Fact]
    public async Task Detrend_RejectsUnknownMethod()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        double[] time = [0.0, 1.0, 2.0];

        double[] flux = [1.0, 2.0, 3.0];

        var fileId = await UploadAsync(SyntheticFits.TimeSeriesBinaryTable(time, flux));

        var response = await _client.PostAsJsonAsync($"/api/timeseries/{fileId}/detrend", new { Method = "sinusoidal" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("timeseries.detrend.unknown_method", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PeriodSearch_SinusoidalLightCurve_RecoversKnownPeriod()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        const double truePeriod = 5.0;

        var time = Enumerable.Range(0, 200).Select(i => i * 0.25).ToArray();

        var flux = time.Select(t => Math.Sin(2.0 * Math.PI * t / truePeriod)).ToArray();

        var fileId = await UploadAsync(SyntheticFits.TimeSeriesBinaryTable(time, flux));

        var response = await _client.GetAsync($"/api/timeseries/{fileId}/period-search?minPeriod=1&maxPeriod=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(truePeriod, body.GetProperty("bestPeriod").GetDouble(), precision: 1);
    }

    [Fact]
    public async Task Transit_BoxShapedDips_RecoversPeriodDepthAndDuration()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        const double truePeriod = 10.0;

        const double trueDepth = 0.05;

        var time = Enumerable.Range(0, 500).Select(i => i * 0.1).ToArray();

        var flux = time.Select(t => (t / truePeriod % 1.0) < 0.05 ? 1.0 - trueDepth : 1.0).ToArray();

        var fileId = await UploadAsync(SyntheticFits.TimeSeriesBinaryTable(time, flux));

        var response = await _client.GetAsync($"/api/timeseries/{fileId}/transit?minPeriod=5&maxPeriod=15&minTransitDepth=0.01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(truePeriod, body.GetProperty("bestPeriod").GetDouble(), precision: 0);

        Assert.True(body.GetProperty("transitDepth").GetDouble() is > 0.02 and < 0.08);
    }

    [Fact]
    public async Task Transit_NoDipsAboveThreshold_ReturnsNotFound()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        var time = Enumerable.Range(0, 100).Select(i => i * 0.1).ToArray();

        var flux = Enumerable.Repeat(1.0, 100).ToArray();

        var fileId = await UploadAsync(SyntheticFits.TimeSeriesBinaryTable(time, flux));

        var response = await _client.GetAsync($"/api/timeseries/{fileId}/transit?minPeriod=1&maxPeriod=5&minTransitDepth=0.01");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Compare_PerfectlyCorrelatedLightCurves_ReturnsUnitCorrelation()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        double[] time = [0.0, 1.0, 2.0, 3.0];

        var primaryFileId = await UploadAsync(SyntheticFits.TimeSeriesBinaryTable(time, [100.0, 200.0, 300.0, 400.0]));

        var comparisonFileId = await UploadAsync(SyntheticFits.TimeSeriesBinaryTable(time, [50.0, 100.0, 150.0, 200.0]));

        var response = await _client.PostAsJsonAsync(
            $"/api/timeseries/{primaryFileId}/compare", new { ComparisonFileIds = new[] { comparisonFileId } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var entry = body.GetProperty("comparisons").EnumerateArray().Single();

        Assert.Equal(1.0, entry.GetProperty("correlationCoefficient").GetDouble(), precision: 6);

        Assert.Equal(-2.5 * Math.Log10(2.0), entry.GetProperty("meanMagnitudeDifference").GetDouble(), precision: 6);

        Assert.Equal(2.0, entry.GetProperty("fluxRatio").GetDouble(), precision: 6);
    }

    [Fact]
    public async Task Compare_MismatchedSampleCounts_ReturnsBadRequest()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        var primaryFileId = await UploadAsync(SyntheticFits.TimeSeriesBinaryTable([0.0, 1.0, 2.0], [1.0, 2.0, 3.0]));

        var comparisonFileId = await UploadAsync(SyntheticFits.TimeSeriesBinaryTable([0.0, 1.0], [1.0, 2.0]));

        var response = await _client.PostAsJsonAsync(
            $"/api/timeseries/{primaryFileId}/compare", new { ComparisonFileIds = new[] { comparisonFileId } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("timeseries.compare.length_mismatch", body.GetProperty("title").GetString());
    }

    private async Task<string> UploadAsync(byte[] fitsBytes)
    {
        using var content = new ByteArrayContent(fitsBytes);

        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var response = await _client.PostAsync("/api/fits/upload", content);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        return body.GetProperty("fileId").GetString()!;
    }
}
