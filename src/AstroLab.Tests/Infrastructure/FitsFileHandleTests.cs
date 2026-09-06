using AstroLab.Infrastructure.Fits;
using AstroLab.Tests.Features;

namespace AstroLab.Tests.Infrastructure;

public class FitsFileHandleTests
{
    [Fact]
    public void Open_ValidFitsFile_Succeeds()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        var path = WriteTempFile(SyntheticFits.SmallGradientImage());

        try
        {
            var result = FitsFileHandle.Open(path);

            Assert.True(result.IsSuccess);

            result.Value.Dispose();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Open_MissingFile_ReturnsInfrastructureFailure()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.fits");

        var result = FitsFileHandle.Open(path);

        Assert.True(result.IsFailure);
        Assert.Equal("fits.cfitsio.open_failed", result.Error.Code);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        if (!CfitsIoNativeAvailability.IsAvailable)
        {
            Assert.Skip("cfitsio native library is not available on this machine.");
        }

        var path = WriteTempFile(SyntheticFits.SmallGradientImage());

        try
        {
            var handle = FitsFileHandle.Open(path).Value;

            handle.Dispose();

            var exception = Record.Exception(handle.Dispose);

            Assert.Null(exception);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteTempFile(byte[] bytes)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.fits");

        File.WriteAllBytes(path, bytes);

        return path;
    }
}
