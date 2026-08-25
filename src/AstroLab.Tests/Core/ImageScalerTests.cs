using AstroLab.Core.Imaging;

namespace AstroLab.Tests.Core;

public class ImageScalerTests
{
    [Theory]
    [InlineData(StretchMode.Linear)]
    [InlineData(StretchMode.Logarithmic)]
    [InlineData(StretchMode.SquareRoot)]
    [InlineData(StretchMode.Asinh)]
    public void Stretch_MapsBlackAndWhitePoints_ToZeroAndMax_RegardlessOfMode(StretchMode mode)
    {
        var parameters = ScaleParametersFactory.Create(blackPoint: 0, whitePoint: 10, mode: mode, asinhSoftening: 0.1);

        ReadOnlySpan<float> source = [0f, 10f];

        Span<byte> destination = stackalloc byte[2];

        var result = ImageScaler.Stretch(source, destination, parameters);

        Assert.True(result.IsSuccess);

        Assert.Equal(0, destination[0]);

        Assert.Equal(255, destination[1]);
    }

    [Fact]
    public void Stretch_Linear_IsProportional()
    {
        var parameters = ScaleParametersFactory.Create(blackPoint: 0, whitePoint: 10, mode: StretchMode.Linear);

        ReadOnlySpan<float> source = [0f, 5f, 10f];

        Span<byte> destination = stackalloc byte[3];

        ImageScaler.Stretch(source, destination, parameters);

        Assert.Equal(0, destination[0]);

        Assert.Equal(128, destination[1]);

        Assert.Equal(255, destination[2]);
    }

    [Fact]
    public void Stretch_SquareRoot_ExpandsFaintDetail()
    {
        var parameters = ScaleParametersFactory.Create(blackPoint: 0, whitePoint: 4, mode: StretchMode.SquareRoot);

        var normalized = ImageScaler.NormalizeAndStretch(1f, parameters);

        Assert.Equal(0.5, normalized, precision: 6);
    }

    [Fact]
    public void Stretch_ClampsValuesOutsideBlackWhiteRange()
    {
        var parameters = ScaleParametersFactory.Create(blackPoint: 0, whitePoint: 10, mode: StretchMode.Linear);

        ReadOnlySpan<float> source = [-100f, 1000f];

        Span<byte> destination = stackalloc byte[2];

        ImageScaler.Stretch(source, destination, parameters);

        Assert.Equal(0, destination[0]);

        Assert.Equal(255, destination[1]);
    }

    [Fact]
    public void Stretch_NonFinitePixels_MapToZero()
    {
        var parameters = ScaleParametersFactory.Create(blackPoint: 0, whitePoint: 10, mode: StretchMode.Linear);

        ReadOnlySpan<float> source = [float.NaN, float.PositiveInfinity, float.NegativeInfinity];

        Span<byte> destination = stackalloc byte[3];

        ImageScaler.Stretch(source, destination, parameters);

        Assert.Equal(0, destination[0]);

        Assert.Equal(0, destination[1]);

        Assert.Equal(0, destination[2]);
    }

    [Fact]
    public void Stretch_RejectsMismatchedBufferLengths()
    {
        var parameters = ScaleParametersFactory.Create(0, 10);

        ReadOnlySpan<float> source = [1f, 2f, 3f];

        Span<byte> destination = stackalloc byte[2];

        var result = ImageScaler.Stretch(source, destination, parameters);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.buffer_length_mismatch", result.Error.Code);
    }

    [Fact]
    public void Stretch_RejectsNonPositiveRange()
    {
        var parameters = ScaleParametersFactory.Create(10, 10);

        ReadOnlySpan<float> source = [5f];

        Span<byte> destination = stackalloc byte[1];

        var result = ImageScaler.Stretch(source, destination, parameters);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.invalid_scale_range", result.Error.Code);
    }
}
