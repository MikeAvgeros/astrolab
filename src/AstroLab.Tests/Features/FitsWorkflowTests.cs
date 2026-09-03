using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AstroLab.Tests.Features;

/// <summary>
/// End-to-end integration tests that exercise the real API host (via <see cref="ApiFactory"/>)
/// across the whole pipeline: upload -> header inspection -> statistics -> PNG rendering ->
/// photometry -> spectral extraction. Uses <see cref="SyntheticFits.SmallGradientImage"/>, a
/// hand-checkable 4x2 8-bit image, so downstream numeric results can be asserted exactly where
/// the underlying algorithm is already unit-tested for correctness in <c>AstroLab.Tests.Core</c>.
/// </summary>
public class FitsWorkflowTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public FitsWorkflowTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> UploadGradientImageAsync() => await UploadAsync(SyntheticFits.SmallGradientImage());

    private async Task<string> UploadGradientSpectrumFrameAsync() => await UploadAsync(SyntheticFits.SmallGradientSpectrumFrame());

    private async Task<string> UploadAsync(byte[] fitsBytes)
    {
        using var content = new ByteArrayContent(fitsBytes);
        
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var response = await _client.PostAsync("/api/fits/upload", content);
        
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        return body.GetProperty("fileId").GetString()!;
    }

    [Fact]
    public async Task Upload_ReturnsCreatedWithFileIdAndSize()
    {
        using var content = new ByteArrayContent(SyntheticFits.SmallGradientImage());
        
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var response = await _client.PostAsync("/api/fits/upload", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("fileId").GetString()));
        
        Assert.True(body.GetProperty("sizeBytes").GetInt64() > 0);
    }

    [Fact]
    public async Task GetHeader_ReturnsParsedKeywords()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/fits/{fileId}/header");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        var keywords = body.GetProperty("keywords").EnumerateArray()
            .ToDictionary(k => k.GetProperty("name").GetString()!, k => k.GetProperty("value").GetString());

        Assert.Equal("8", keywords["BITPIX"]);
        
        Assert.Equal("4", keywords["NAXIS1"]);
        
        Assert.Equal("2", keywords["NAXIS2"]);
    }

    [Fact]
    public async Task GetHeader_UnknownFileId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/fits/does-not-exist/header");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHeader_EmptyFile_ReturnsBadRequestInsteadOfCrashing()
    {
        var fileId = await UploadAsync([]);

        var response = await _client.GetAsync($"/api/fits/{fileId}/header");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.Equal("fits.header.empty_file", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetHeader_UnrelatedExtensionSpectralMarker_DoesNotMisclassifyLoadedImage()
    {
        var fileId = await UploadAsync(SyntheticFits.MultiHduImageWithUnrelatedSpectralMarker());

        var headerResponse = await _client.GetAsync($"/api/fits/{fileId}/header");
        
        var headerBody = await headerResponse.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.Equal("Image", headerBody.GetProperty("datasetKind").GetString());

        var statisticsResponse = await _client.GetAsync($"/api/images/{fileId}/statistics");
        
        Assert.Equal(HttpStatusCode.OK, statisticsResponse.StatusCode);
    }

    [Fact]
    public async Task GetHeader_ReportsImageDatasetKind()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/fits/{fileId}/header");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Image", body.GetProperty("datasetKind").GetString());

        Assert.Equal(1, body.GetProperty("hdus").GetArrayLength());
    }

    [Fact]
    public async Task GetHeader_ReportsPerHduDataTypeAndAxesAndOwnHeader()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/fits/{fileId}/header");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var hdu = body.GetProperty("hdus")[0];

        Assert.Equal(0, hdu.GetProperty("index").GetInt32());

        Assert.Equal("Primary", hdu.GetProperty("type").GetString());

        Assert.Equal("Byte", hdu.GetProperty("dataType").GetString());

        Assert.Equal(2, hdu.GetProperty("numberOfAxes").GetInt32());

        Assert.Equal([4, 2], hdu.GetProperty("axisDimensions").EnumerateArray().Select(e => e.GetInt32()));

        var ownKeywords = hdu.GetProperty("header").EnumerateArray()
            .Select(k => k.GetProperty("name").GetString()!)
            .ToArray();

        Assert.Contains("NAXIS1", ownKeywords);
    }

    [Fact]
    public async Task GetHeader_ExposesCommonMetadataAsNullWhenAbsent()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/fits/{fileId}/header");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var commonMetadata = body.GetProperty("commonMetadata");

        Assert.Equal(JsonValueKind.Null, commonMetadata.GetProperty("object").ValueKind);

        Assert.Equal(JsonValueKind.Null, commonMetadata.GetProperty("telescope").ValueKind);
    }

    [Fact]
    public async Task GetHeader_ReportsSpectrumDatasetKind()
    {
        var fileId = await UploadGradientSpectrumFrameAsync();

        var response = await _client.GetAsync($"/api/fits/{fileId}/header");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.Equal("Spectrum", body.GetProperty("datasetKind").GetString());
    }

    [Fact]
    public async Task GetStatistics_ComputesExactMomentsForKnownImage()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/statistics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(10.0, body.GetProperty("min").GetDouble(), precision: 6);
        
        Assert.Equal(80.0, body.GetProperty("max").GetDouble(), precision: 6);
        
        Assert.Equal(45.0, body.GetProperty("mean").GetDouble(), precision: 6);
        
        Assert.Equal(8, body.GetProperty("validPixelCount").GetInt64());
        
        Assert.Equal(0, body.GetProperty("invalidPixelCount").GetInt64());
        
        Assert.Equal(0.0, body.GetProperty("deadPixelPercentage").GetDouble(), precision: 6);
        
        Assert.True(body.GetProperty("skySigma").GetDouble() > 0);
    }

    [Fact]
    public async Task GetStatistics_ReportsMedianAndPercentileSuite()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/statistics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var median = body.GetProperty("median").GetDouble();

        Assert.True(median is >= 10.0 and <= 80.0);

        var percentiles = body.GetProperty("percentiles").EnumerateArray()
            .Select(p => (Percentile: p.GetProperty("percentile").GetDouble(), Value: p.GetProperty("value").GetDouble()))
            .ToArray();

        Assert.Equal([1.0, 5.0, 25.0, 50.0, 75.0, 95.0, 99.0], percentiles.Select(p => p.Percentile));

        for (var i = 1; i < percentiles.Length; i++)
        {
            Assert.True(percentiles[i].Value >= percentiles[i - 1].Value);
        }

        Assert.True(percentiles[0].Value >= 10.0 && percentiles[^1].Value <= 80.0);
    }

    [Fact]
    public async Task GetHistogram_ReturnsBinsCoveringTheFullPixelRange()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/histogram?binCount=4");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(4, body.GetProperty("binCount").GetInt32());

        var binEdges = body.GetProperty("binEdges").EnumerateArray().Select(e => e.GetDouble()).ToArray();

        Assert.Equal(5, binEdges.Length);

        Assert.Equal(10.0, binEdges[0], precision: 6);

        Assert.Equal(80.0, binEdges[^1], precision: 6);

        var counts = body.GetProperty("counts").EnumerateArray().Select(c => c.GetInt64()).ToArray();

        Assert.Equal(8, counts.Sum());

        Assert.Equal(8, body.GetProperty("validPixelCount").GetInt64());
    }

    [Fact]
    public async Task RenderPng_ReturnsValidPngBytes()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/render?stretch=Linear&blackPoint=0&whitePoint=80");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        
        Assert.Equal([137, 80, 78, 71, 13, 10, 26, 10], bytes[..8]);
    }

    [Fact]
    public async Task MeasureAperture_ReturnsPositiveFiniteFlux()
    {
        var fileId = await UploadGradientImageAsync();
        
        var request = new
        {
            CenterX = 0.5,
            CenterY = 0.5,
            ApertureRadius = 0.3,
            AnnulusInnerRadius = 1.0,
            AnnulusOuterRadius = 1.8,
        };

        var response = await _client.PostAsJsonAsync($"/api/images/{fileId}/photometry/aperture", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        var rawFlux = body.GetProperty("rawFlux").GetDouble();
        
        Assert.True(rawFlux > 0 && double.IsFinite(rawFlux));
    }

    [Fact]
    public async Task MeasureAperture_OnSpectrumFrame_ReturnsBadRequest()
    {
        var fileId = await UploadGradientSpectrumFrameAsync();
        
        var request = new
        {
            CenterX = 0.5,
            CenterY = 0.5,
            ApertureRadius = 0.3,
            AnnulusInnerRadius = 1.0,
            AnnulusOuterRadius = 1.8,
        };

        var response = await _client.PostAsJsonAsync($"/api/images/{fileId}/photometry/aperture", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.Equal("fits.data.unsupported_type", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ExtractSpectrum_SumsRowsPerColumnExactly()
    {
        var fileId = await UploadGradientSpectrumFrameAsync();
        
        var request = new
        {
            Axis = "Horizontal",
            TraceCenters = new[] { 1.0, 1.0, 1.0, 1.0 },
            ApertureHalfWidth = 1.0,
        };

        var response = await _client.PostAsJsonAsync($"/api/spectroscopy/{fileId}/extract", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        var flux = body.GetProperty("flux").EnumerateArray().Select(e => e.GetDouble()).ToArray();

        Assert.Equal([60.0, 80.0, 100.0, 120.0], flux);
    }

    [Fact]
    public async Task ExtractSpectrum_OnPlainImage_ReturnsBadRequest()
    {
        var fileId = await UploadGradientImageAsync();
        
        var request = new
        {
            Axis = "Horizontal",
            TraceCenters = new[] { 1.0, 1.0, 1.0, 1.0 },
            ApertureHalfWidth = 1.0,
        };

        var response = await _client.PostAsJsonAsync($"/api/spectroscopy/{fileId}/extract", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.Equal("fits.data.unsupported_type", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task SearchObservations_MissingRequiredArchiveParameter_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/archives/search?target=M31");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
