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

    private async Task<string> UploadGradientImageWithWcsAsync() => await UploadAsync(SyntheticFits.SmallGradientImageWithWcs());

    private async Task<string> UploadImageWithSourceAsync() => await UploadAsync(SyntheticFits.SmallImageWithSource());

    private async Task<string> UploadImageWithSourceAndWcsAsync() => await UploadAsync(SyntheticFits.SmallImageWithSourceAndWcs());

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
    public async Task GetBackground_MeshLargerThanImage_MatchesWholeImageMedianAndPositiveRms()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/background");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(64, body.GetProperty("meshSizePixels").GetInt32());

        Assert.Equal(45.0, body.GetProperty("medianBackground").GetDouble(), precision: 6);

        Assert.True(body.GetProperty("backgroundRms").GetDouble() > 0);
    }

    [Fact]
    public async Task GetBackground_RejectsNonPositiveMeshSize()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/background?meshSizePixels=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    public async Task MeasureAllSources_ReturnsPositiveFluxAndFiniteMagnitudeForTheDetectedSource()
    {
        var fileId = await UploadImageWithSourceAsync();

        var response = await _client.GetAsync(
            $"/api/images/{fileId}/photometry/sources?apertureRadius=3&annulusInnerRadius=4&annulusOuterRadius=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var sources = body.GetProperty("sources").EnumerateArray().ToArray();

        var source = Assert.Single(sources);

        Assert.Equal(1, source.GetProperty("sourceId").GetInt32());

        var netFlux = source.GetProperty("netFlux").GetDouble();

        Assert.True(netFlux > 0 && double.IsFinite(netFlux));

        Assert.True(source.GetProperty("fluxUncertainty").GetDouble() > 0);

        Assert.True(double.IsFinite(source.GetProperty("instrumentalMagnitude").GetDouble()));

        Assert.True(source.GetProperty("magnitudeUncertainty").GetDouble() > 0);
    }

    [Fact]
    public async Task MeasureAllSources_RejectsNonPositiveApertureRadius()
    {
        var fileId = await UploadImageWithSourceAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/photometry/sources?apertureRadius=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MeasureDifferential_AtIdenticalTargetAndComparisonPositions_ReturnsZeroDifferential()
    {
        var fileId = await UploadImageWithSourceAsync();

        var request = new
        {
            TargetCenterX = 5.0,
            TargetCenterY = 5.0,
            ComparisonCenterX = 5.0,
            ComparisonCenterY = 5.0,
            ApertureRadius = 1.5,
            AnnulusInnerRadius = 2.0,
            AnnulusOuterRadius = 3.0,
        };

        var response = await _client.PostAsJsonAsync($"/api/images/{fileId}/photometry/differential", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(body.GetProperty("targetMagnitude").GetDouble(), body.GetProperty("comparisonMagnitude").GetDouble(), precision: 9);

        Assert.Equal(0.0, body.GetProperty("differentialMagnitude").GetDouble(), precision: 9);

        Assert.True(body.GetProperty("uncertainty").GetDouble() > 0);
    }

    [Fact]
    public async Task MeasureDifferential_RejectsNonPositiveApertureRadius()
    {
        var fileId = await UploadImageWithSourceAsync();

        var request = new
        {
            TargetCenterX = 5.0,
            TargetCenterY = 5.0,
            ComparisonCenterX = 1.0,
            ComparisonCenterY = 1.0,
            ApertureRadius = 0.0,
            AnnulusInnerRadius = 2.0,
            AnnulusOuterRadius = 3.0,
        };

        var response = await _client.PostAsJsonAsync($"/api/images/{fileId}/photometry/differential", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    public async Task DetectLines_OnFrameWithASingleEmissionSpike_ReportsExactPositionFluxAndFwhm()
    {
        var fileId = await UploadAsync(SyntheticFits.SmallSpectrumWithEmissionLine());

        var response = await _client.GetAsync($"/api/spectroscopy/{fileId}/lines?significanceThreshold=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var line = body.GetProperty("lines").EnumerateArray().Single();

        Assert.Equal(4.0, line.GetProperty("wavelength").GetDouble(), precision: 6);

        Assert.Equal(270.0, line.GetProperty("flux").GetDouble(), precision: 6);

        Assert.Equal(1.0, line.GetProperty("fwhm").GetDouble(), precision: 6);
    }

    [Fact]
    public async Task DetectLines_OnSmoothGradientSpectrum_FindsNoLines()
    {
        var fileId = await UploadGradientSpectrumFrameAsync();

        var response = await _client.GetAsync($"/api/spectroscopy/{fileId}/lines");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Empty(body.GetProperty("lines").EnumerateArray());
    }

    [Fact]
    public async Task DetectLines_OnPlainImage_ReturnsBadRequest()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/spectroscopy/{fileId}/lines");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("fits.data.unsupported_type", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task DetectLines_WithDispersionCoefficients_ReportsPhysicalWavelengthAndEmissionFlag()
    {
        var fileId = await UploadAsync(SyntheticFits.SmallSpectrumWithEmissionLine());

        var response = await _client.GetAsync(
            $"/api/spectroscopy/{fileId}/lines?significanceThreshold=3&dispersionCoefficients=500&dispersionCoefficients=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var line = body.GetProperty("lines").EnumerateArray().Single();

        Assert.Equal(508.0, line.GetProperty("wavelength").GetDouble(), precision: 6);

        Assert.Equal(2.0, line.GetProperty("fwhm").GetDouble(), precision: 6);

        Assert.Equal(4.0, line.GetProperty("binPosition").GetDouble(), precision: 6);

        Assert.True(line.GetProperty("isWavelengthCalibrated").GetBoolean());

        Assert.True(line.GetProperty("isEmission").GetBoolean());
    }

    [Fact]
    public async Task DetectLines_RejectsNonPositiveSignificanceThreshold()
    {
        var fileId = await UploadAsync(SyntheticFits.SmallSpectrumWithEmissionLine());

        var response = await _client.GetAsync($"/api/spectroscopy/{fileId}/lines?significanceThreshold=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EstimateRedshift_ComputesMeanFractionalWavelengthShift()
    {
        var request = new
        {
            ObservedWavelengths = new[] { 505.0, 1020.0 },
            RestWavelengths = new[] { 500.0, 1000.0 },
        };

        var response = await _client.PostAsJsonAsync("/api/spectroscopy/does-not-matter/redshift", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0.015, body.GetProperty("redshift").GetDouble(), precision: 9);

        Assert.Equal(0.005, body.GetProperty("uncertainty").GetDouble(), precision: 9);
    }

    [Fact]
    public async Task EstimateRedshift_RejectsMismatchedLinePairLengths()
    {
        var request = new
        {
            ObservedWavelengths = new[] { 505.0, 1020.0 },
            RestWavelengths = new[] { 500.0 },
        };

        var response = await _client.PostAsJsonAsync("/api/spectroscopy/does-not-matter/redshift", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("spectroscopy.redshift.length_mismatch", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task EstimateRedshift_ByCrossCorrelation_RecoversKnownRedshiftFromShiftedTemplate()
    {
        var wavelengths = Enumerable.Range(0, 201).Select(i => 4900.0 + i).ToArray();

        double RestFrameProfile(double wavelength) => 1.0 - (0.5 * Math.Exp(-Math.Pow(wavelength - 5000.0, 2) / 50.0));

        const double trueRedshift = 0.01;

        var request = new
        {
            ObservedSpectrumWavelengths = wavelengths,
            ObservedFlux = wavelengths.Select(w => RestFrameProfile(w / (1.0 + trueRedshift))).ToArray(),
            TemplateWavelengths = wavelengths,
            TemplateFlux = wavelengths.Select(RestFrameProfile).ToArray(),
            MinRedshift = 0.0,
            MaxRedshift = 0.05,
        };

        var response = await _client.PostAsJsonAsync("/api/spectroscopy/does-not-matter/redshift", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(trueRedshift, body.GetProperty("redshift").GetDouble(), precision: 3);

        Assert.Equal("cross_correlation", body.GetProperty("method").GetString());
    }

    [Fact]
    public async Task CalibrateWavelengths_TwoPixelWavelengthPairs_FitsExactLinearDispersionSolutionAndAppliesItToTheExtractedSpectrum()
    {
        var fileId = await UploadGradientSpectrumFrameAsync();

        var request = new
        {
            PixelPositions = new[] { 0.0, 3.0 },
            KnownWavelengths = new[] { 500.0, 506.0 },
        };

        var response = await _client.PostAsJsonAsync($"/api/spectroscopy/{fileId}/calibrate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var coefficients = body.GetProperty("dispersionCoefficients").EnumerateArray().Select(e => e.GetDouble()).ToArray();

        Assert.Equal([500.0, 2.0], coefficients.Select(c => Math.Round(c, 6)).ToArray());

        Assert.Equal(0.0, body.GetProperty("residualRms").GetDouble(), precision: 6);

        var wavelengths = body.GetProperty("wavelengths").EnumerateArray().Select(e => e.GetDouble()).ToArray();

        Assert.Equal([500.0, 502.0, 504.0, 506.0], wavelengths);

        var flux = body.GetProperty("flux").EnumerateArray().Select(e => e.GetDouble()).ToArray();

        Assert.Equal([60.0, 80.0, 100.0, 120.0], flux);

        Assert.False(body.GetProperty("fluxCalibrated").GetBoolean());
    }

    [Fact]
    public async Task CalibrateWavelengths_WithFluxSensitivityCurve_DividesExtractedFluxBySensitivity()
    {
        var fileId = await UploadGradientSpectrumFrameAsync();

        var request = new
        {
            PixelPositions = new[] { 0.0, 3.0 },
            KnownWavelengths = new[] { 500.0, 506.0 },
            FluxSensitivity = new[] { 2.0, 2.0, 2.0, 2.0 },
        };

        var response = await _client.PostAsJsonAsync($"/api/spectroscopy/{fileId}/calibrate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var flux = body.GetProperty("flux").EnumerateArray().Select(e => e.GetDouble()).ToArray();

        Assert.Equal([30.0, 40.0, 50.0, 60.0], flux);

        Assert.True(body.GetProperty("fluxCalibrated").GetBoolean());
    }

    [Fact]
    public async Task CalibrateWavelengths_RejectsFewerThanTwoPixelWavelengthPairs()
    {
        var request = new
        {
            PixelPositions = new[] { 0.0 },
            KnownWavelengths = new[] { 500.0 },
        };

        var response = await _client.PostAsJsonAsync("/api/spectroscopy/does-not-matter/calibrate", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("spectroscopy.calibration_insufficient_points", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task CompareSpectra_IdenticalSpectra_ReturnsUnitCorrelationAndZeroShift()
    {
        var primaryFileId = await UploadAsync(SyntheticFits.SmallSpectrumWithEmissionLineAndDispersionWcs());

        var comparisonFileId = await UploadAsync(SyntheticFits.SmallSpectrumWithEmissionLineAndDispersionWcs());

        var response = await _client.PostAsJsonAsync(
            $"/api/spectroscopy/{primaryFileId}/compare", new { ComparisonFileId = comparisonFileId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1.0, body.GetProperty("crossCorrelationPeak").GetDouble(), precision: 6);

        Assert.Equal(0.0, body.GetProperty("velocityShiftKmPerSec").GetDouble(), precision: 6);

        Assert.Equal(1.0, body.GetProperty("meanFluxRatio").GetDouble(), precision: 6);

        Assert.Equal(0.0, body.GetProperty("rmsFluxDifference").GetDouble(), precision: 6);
    }

    [Fact]
    public async Task CompareSpectra_NoDispersionWcs_ReturnsBadRequest()
    {
        var primaryFileId = await UploadAsync(SyntheticFits.SmallSpectrumWithEmissionLine());

        var comparisonFileId = await UploadAsync(SyntheticFits.SmallSpectrumWithEmissionLine());

        var response = await _client.PostAsJsonAsync(
            $"/api/spectroscopy/{primaryFileId}/compare", new { ComparisonFileId = comparisonFileId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("spectroscopy.compare.no_wavelength_solution", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task SearchObservations_MissingRequiredArchiveParameter_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/archives/search?target=M31");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchObservations_MissingRequiredTargetParameter_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/archives/search?archive=Eso");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetWcs_ReturnsProjectionAndReferenceMetadata()
    {
        var fileId = await UploadGradientImageWithWcsAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/astrometry/wcs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Tan", body.GetProperty("projection").GetString());

        Assert.Equal("ICRS", body.GetProperty("coordinateSystem").GetString());

        Assert.Equal(180.0, body.GetProperty("referenceRightAscension").GetDouble(), precision: 6);

        Assert.Equal(0.0, body.GetProperty("referenceDeclination").GetDouble(), precision: 6);
    }

    [Fact]
    public async Task GetWcs_OnImageWithoutWcs_ReturnsNotFound()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/astrometry/wcs");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PixelToWorld_AtReferencePixel_ReturnsReferenceCoordinates()
    {
        var fileId = await UploadGradientImageWithWcsAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/astrometry/pixel-to-world?pixelX=0.5&pixelY=0.5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(180.0, body.GetProperty("rightAscension").GetDouble(), precision: 6);

        Assert.Equal(0.0, body.GetProperty("declination").GetDouble(), precision: 6);
    }

    [Fact]
    public async Task PixelToWorld_ThenWorldToPixel_RoundTripsThroughTheApi()
    {
        var fileId = await UploadGradientImageWithWcsAsync();

        var toWorldResponse = await _client.GetAsync($"/api/images/{fileId}/astrometry/pixel-to-world?pixelX=2.5&pixelY=1.5");

        Assert.Equal(HttpStatusCode.OK, toWorldResponse.StatusCode);

        var world = await toWorldResponse.Content.ReadFromJsonAsync<JsonElement>();

        var ra = world.GetProperty("rightAscension").GetDouble();

        var dec = world.GetProperty("declination").GetDouble();

        var toPixelResponse = await _client.GetAsync(
            $"/api/images/{fileId}/astrometry/world-to-pixel?rightAscension={ra:R}&declination={dec:R}");

        Assert.Equal(HttpStatusCode.OK, toPixelResponse.StatusCode);

        var pixel = await toPixelResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(2.5, pixel.GetProperty("pixelX").GetDouble(), precision: 4);

        Assert.Equal(1.5, pixel.GetProperty("pixelY").GetDouble(), precision: 4);
    }

    [Fact]
    public async Task PixelToWorld_OnImageWithoutWcs_ReturnsNotFound()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/astrometry/pixel-to-world?pixelX=0&pixelY=0");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WorldToPixel_RejectsOutOfRangeDeclination()
    {
        var fileId = await UploadGradientImageWithWcsAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/astrometry/world-to-pixel?rightAscension=180&declination=120");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetFootprint_ReturnsFourCornersMatchingPixelToWorldAtImageEdges()
    {
        var fileId = await UploadGradientImageWithWcsAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/astrometry/footprint");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var corners = body.GetProperty("corners").EnumerateArray().ToArray();

        Assert.Equal(4, corners.Length);

        // SmallGradientImageWithWcs is 4x2, so its pixel-index corners are (0,0), (3,0), (0,1), (3,1).
        double[] cornerPixelsX = [0, 3, 0, 3];

        double[] cornerPixelsY = [0, 0, 1, 1];

        for (var i = 0; i < corners.Length; i++)
        {
            var expectedResponse = await _client.GetAsync(
                $"/api/images/{fileId}/astrometry/pixel-to-world?pixelX={cornerPixelsX[i]:R}&pixelY={cornerPixelsY[i]:R}");

            Assert.Equal(HttpStatusCode.OK, expectedResponse.StatusCode);

            var expected = await expectedResponse.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(expected.GetProperty("rightAscension").GetDouble(), corners[i].GetProperty("rightAscension").GetDouble(), precision: 9);

            Assert.Equal(expected.GetProperty("declination").GetDouble(), corners[i].GetProperty("declination").GetDouble(), precision: 9);
        }
    }

    [Fact]
    public async Task GetFootprint_OnImageWithoutWcs_ReturnsNotFound()
    {
        var fileId = await UploadGradientImageAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/astrometry/footprint");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DetectSources_FindsOneSourceAtExpectedCentroidAndPixelCount_WithNullCoordinatesWithoutWcs()
    {
        var fileId = await UploadImageWithSourceAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/sources");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var sources = body.GetProperty("sources").EnumerateArray().ToArray();

        Assert.Single(sources);

        var source = sources[0];

        Assert.Equal(1, source.GetProperty("id").GetInt32());

        Assert.Equal(9, source.GetProperty("pixelCount").GetInt32());

        Assert.Equal(200.0, source.GetProperty("peakValue").GetDouble(), precision: 6);

        Assert.Equal(5.5, source.GetProperty("pixelX").GetDouble(), precision: 6);

        Assert.Equal(5.5, source.GetProperty("pixelY").GetDouble(), precision: 6);

        Assert.Equal(JsonValueKind.Null, source.GetProperty("rightAscension").ValueKind);

        Assert.Equal(JsonValueKind.Null, source.GetProperty("declination").ValueKind);
    }

    [Fact]
    public async Task DetectSources_WithWcs_ResolvesRightAscensionAndDeclination()
    {
        var fileId = await UploadImageWithSourceAndWcsAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/sources");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var source = body.GetProperty("sources").EnumerateArray().Single();

        Assert.NotEqual(JsonValueKind.Null, source.GetProperty("rightAscension").ValueKind);

        Assert.NotEqual(JsonValueKind.Null, source.GetProperty("declination").ValueKind);

        var ra = source.GetProperty("rightAscension").GetDouble();

        var dec = source.GetProperty("declination").GetDouble();

        Assert.True(ra is > 179.0 and < 181.0);

        Assert.True(dec is > -1.0 and < 1.0);
    }

    [Fact]
    public async Task DetectSources_WithMinimumAreaAboveBlockSize_ReturnsNoSources()
    {
        var fileId = await UploadImageWithSourceAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/sources?minimumArea=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Empty(body.GetProperty("sources").EnumerateArray());
    }

    [Fact]
    public async Task DetectSources_RejectsNonPositiveThreshold()
    {
        var fileId = await UploadImageWithSourceAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/sources?thresholdSigma=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DetectSources_OnSpectrumFrame_ReturnsBadRequest()
    {
        var fileId = await UploadGradientSpectrumFrameAsync();

        var response = await _client.GetAsync($"/api/images/{fileId}/sources");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
