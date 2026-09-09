using AstroLab.Core.Imaging;

namespace AstroLab.Tests.Core;

public class ImageBackgroundModellerTests
{
    [Fact]
    public void Model_MeshLargerThanImage_MatchesWholeImageMedianAndIqrSigma()
    {
        ReadOnlySpan<float> pixels = [10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f];

        var stats = ImageStatistics.Compute(pixels).Value;

        const int histogramBinsMatchingModeller = 8;

        Span<double> quartiles = stackalloc double[2];

        ImageStatistics.ComputePercentiles(pixels, stats, [25.0, 75.0], quartiles, histogramBinsMatchingModeller);

        Span<double> median = stackalloc double[1];

        ImageStatistics.ComputePercentiles(pixels, stats, [50.0], median, histogramBinsMatchingModeller);

        var expectedSigma = (quartiles[1] - quartiles[0]) / 1.349;

        var result = ImageBackgroundModeller.Model(pixels, width: 4, height: 2, meshSizePixels: 64);

        Assert.True(result.IsSuccess);

        var model = result.Value;

        Assert.Equal(1, model.MeshCountX);

        Assert.Equal(1, model.MeshCountY);

        Assert.Equal(median[0], model.MedianBackground, precision: 6);

        Assert.Equal(expectedSigma, model.BackgroundRms, precision: 6);
    }

    [Fact]
    public void Model_PartitionsIntoMeshGrid_ReportsMedianOfPerMeshBackgrounds()
    {
        const int width = 4;

        const int height = 4;

        var pixels = new float[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var meshX = x / 2;

                var meshY = y / 2;

                pixels[(y * width) + x] = ((meshY * 2) + meshX) switch
                {
                    0 => 10f,
                    1 => 20f,
                    2 => 30f,
                    _ => 40f,
                };
            }
        }

        var result = ImageBackgroundModeller.Model(pixels, width, height, meshSizePixels: 2);

        Assert.True(result.IsSuccess);

        var model = result.Value;

        Assert.Equal(2, model.MeshCountX);

        Assert.Equal(2, model.MeshCountY);

        Assert.Equal(25.0, model.MedianBackground, precision: 9);

        Assert.Equal(0.0, model.BackgroundRms, precision: 9);
    }

    [Fact]
    public void Model_SkipsMeshBoxesWithNoValidPixels()
    {
        const int width = 4;

        const int height = 2;

        float[] pixels =
        [
            float.NaN, float.NaN, 50f, 50f,
            float.NaN, float.NaN, 50f, 50f,
        ];

        var result = ImageBackgroundModeller.Model(pixels, width, height, meshSizePixels: 2);

        Assert.True(result.IsSuccess);

        var model = result.Value;

        Assert.Equal(50.0, model.MedianBackground, precision: 9);

        Assert.Equal(0.0, model.BackgroundRms, precision: 9);
    }

    [Fact]
    public void Model_HandlesImageDimensionsNotDivisibleByMeshSize()
    {
        const int width = 5;

        const int height = 3;

        var pixels = new float[width * height];

        Array.Fill(pixels, 42f);

        var result = ImageBackgroundModeller.Model(pixels, width, height, meshSizePixels: 2);

        Assert.True(result.IsSuccess);

        var model = result.Value;

        Assert.Equal(3, model.MeshCountX);

        Assert.Equal(2, model.MeshCountY);

        Assert.Equal(42.0, model.MedianBackground, precision: 9);

        Assert.Equal(0.0, model.BackgroundRms, precision: 9);
    }

    [Fact]
    public void Model_RejectsNonPositiveMeshSize()
    {
        ReadOnlySpan<float> pixels = [1f, 2f, 3f, 4f];

        var result = ImageBackgroundModeller.Model(pixels, width: 2, height: 2, meshSizePixels: 0);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.background.invalid_mesh_size", result.Error.Code);
    }

    [Fact]
    public void Model_RejectsMismatchedImageBounds()
    {
        ReadOnlySpan<float> pixels = [1f, 2f, 3f];

        var result = ImageBackgroundModeller.Model(pixels, width: 2, height: 2, meshSizePixels: 2);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.background.invalid_image_bounds", result.Error.Code);
    }

    [Fact]
    public void Model_AllPixelsNonFinite_Fails()
    {
        float[] pixels = [float.NaN, float.NaN, float.NaN, float.NaN];

        var result = ImageBackgroundModeller.Model(pixels, width: 2, height: 2, meshSizePixels: 2);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.background.no_valid_meshes", result.Error.Code);
    }
}
