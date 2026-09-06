using AstroLab.Core.Fits;
using AstroLab.Infrastructure.Storage;
using AstroLab.Tests.Features;

namespace AstroLab.Tests.Infrastructure;

public class CfitsIoTimeSeriesReaderTests
{
    [Fact]
    public async Task ReadAsync_ValidTimeSeriesTable_ReturnsMatchingRows()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        double[] time = [0.0, 1.5, 3.0];
        double[] flux = [10.0, 20.0, 30.0];

        var path = WriteTempFile(SyntheticFits.TimeSeriesBinaryTable(time, flux));

        try
        {
            var header = FitsHeader.Parse(ReadHeaderBlock(path, hduIndex: 1)).Value;

            var hdu = HduDescriptor.FromHeader(1, header).Value;

            var descriptor = TimeSeriesTableDescriptor.Resolve(hdu).Value;

            var result = await CfitsIoTimeSeriesReader.ReadAsync(path, hduNumber: 2, descriptor, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(time, result.Value.Time);
            Assert.Equal(flux, result.Value.Flux);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAsync_MissingFile_ReturnsInfrastructureFailure()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        var existingPath = WriteTempFile(SyntheticFits.TimeSeriesBinaryTable([1.0], [1.0]));

        var header = FitsHeader.Parse(ReadHeaderBlock(existingPath, hduIndex: 1)).Value;

        var descriptor = TimeSeriesTableDescriptor.Resolve(HduDescriptor.FromHeader(1, header).Value).Value;

        File.Delete(existingPath);

        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.fits");

        var result = await CfitsIoTimeSeriesReader.ReadAsync(missingPath, hduNumber: 2, descriptor, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("fits.cfitsio.open_failed", result.Error.Code);
    }

    private static string WriteTempFile(byte[] bytes)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.fits");

        File.WriteAllBytes(path, bytes);

        return path;
    }

    private static byte[] ReadHeaderBlock(string path, int hduIndex)
    {
        const int blockSize = 2880;

        var bytes = File.ReadAllBytes(path);

        return bytes.AsSpan(hduIndex * blockSize, blockSize).ToArray();
    }
}
