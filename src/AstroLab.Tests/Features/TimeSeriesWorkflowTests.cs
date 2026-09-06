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
