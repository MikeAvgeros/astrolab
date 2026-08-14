using AstroLab.Core.Imaging;
using AstroLab.Core.Photometry;
using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

/// <summary>
/// Verifies that the hot data-processing paths in <c>AstroLab.Core</c> perform zero managed-heap
/// allocations once JIT-warmed. Test-harness allocations (array/buffer setup, delegate creation)
/// happen strictly outside the measured window so only the algorithm itself is attributed.
/// </summary>
public class AllocationTests
{
    private static long MeasureAllocatedBytes(Action action)
    {
        action();

        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        var after = GC.GetAllocatedBytesForCurrentThread();
        return after - before;
    }

    [Fact]
    public void ImageScaler_Stretch_AllocatesNoManagedMemory()
    {
        var source = new float[50_000];
        for (var i = 0; i < source.Length; i++)
        {
            source[i] = i % 1000;
        }

        var destination = new byte[source.Length];
        var parameters = new ScaleParameters(0, 999, StretchMode.Asinh, 0.1);

        var allocated = MeasureAllocatedBytes(() => ImageScaler.Stretch(source, destination, parameters));

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void ColorMapper_Apply_AllocatesNoManagedMemory()
    {
        var intensities = new byte[50_000];
        for (var i = 0; i < intensities.Length; i++)
        {
            intensities[i] = (byte)(i % 256);
        }

        var rgb = new byte[intensities.Length * 3];

        var allocated = MeasureAllocatedBytes(() => ColorMapper.Apply(intensities, rgb, ColorMap.Viridis));

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void ImageStatistics_Compute_AllocatesNoManagedMemory()
    {
        var pixels = new float[50_000];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = i % 1000;
        }

        var allocated = MeasureAllocatedBytes(() => ImageStatistics.Compute(pixels));

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void ApertureEngine_MeasureCircularAperture_AllocatesNoManagedMemory()
    {
        const int size = 201;
        var pixels = new float[size * size];
        Array.Fill(pixels, 3.0f);

        var allocated = MeasureAllocatedBytes(() =>
            ApertureEngine.MeasureCircularAperture(pixels, size, size, 100.0, 100.0, 40.0));

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void ApertureEngine_MeasureAnnulusBackground_Mean_AllocatesNoManagedMemory()
    {
        const int size = 201;
        var pixels = new float[size * size];
        Array.Fill(pixels, 3.0f);

        var allocated = MeasureAllocatedBytes(() =>
            ApertureEngine.MeasureAnnulusBackground(pixels, size, size, 100.0, 100.0, 40.0, 60.0, BackgroundEstimationMethod.Mean));

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void ApertureEngine_MeasureAnnulusBackground_Median_ReusesPooledBuffer_AfterWarmup()
    {
        const int size = 201;
        var pixels = new float[size * size];
        Array.Fill(pixels, 3.0f);

        var allocated = MeasureAllocatedBytes(() =>
            ApertureEngine.MeasureAnnulusBackground(pixels, size, size, 100.0, 100.0, 40.0, 60.0, BackgroundEstimationMethod.Median));

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void SpectrumExtractor_ExtractBoxcar_AllocatesNoManagedMemory()
    {
        const int width = 500;
        const int height = 50;
        var image = new float[width * height];
        Array.Fill(image, 7.0f);

        var traceCenters = new double[width];
        Array.Fill(traceCenters, 25.0);
        var spectrum = new double[width];

        var allocated = MeasureAllocatedBytes(() =>
            SpectrumExtractor.ExtractBoxcar(image, width, height, DispersionAxis.Horizontal, traceCenters, 5.0, spectrum));

        Assert.Equal(0, allocated);
    }
}
