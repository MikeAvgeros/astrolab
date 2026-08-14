using AstroLab.Core.Imaging;

namespace AstroLab.Tests.Core;

public class ColorMapperTests
{
    [Fact]
    public void Map_Grayscale_ReturnsEqualChannels()
    {
        var (r, g, b) = ColorMapper.Map(128, ColorMap.Grayscale);

        Assert.Equal(128, r);
        Assert.Equal(128, g);
        Assert.Equal(128, b);
    }

    [Fact]
    public void Map_Hot_IsBlackAtZero_AndWhiteAtMax()
    {
        var black = ColorMapper.Map(0, ColorMap.Hot);
        var white = ColorMapper.Map(255, ColorMap.Hot);

        Assert.Equal((0, 0, 0), black);
        Assert.Equal((255, 255, 255), white);
    }

    [Fact]
    public void Map_Viridis_MatchesEndpointStops()
    {
        var start = ColorMapper.Map(0, ColorMap.Viridis);
        var end = ColorMapper.Map(255, ColorMap.Viridis);

        Assert.Equal((68, 1, 84), start);
        Assert.Equal((253, 231, 37), end);
    }

    [Fact]
    public void Apply_WritesInterleavedRgbTriples_ForEachIntensity()
    {
        ReadOnlySpan<byte> intensities = [0, 255];
        Span<byte> rgb = stackalloc byte[6];

        var result = ColorMapper.Apply(intensities, rgb, ColorMap.Grayscale);

        Assert.True(result.IsSuccess);
        Assert.Equal([0, 0, 0, 255, 255, 255], rgb.ToArray());
    }

    [Fact]
    public void Apply_RejectsMismatchedBufferLength()
    {
        ReadOnlySpan<byte> intensities = [0, 255];
        Span<byte> rgb = stackalloc byte[5];

        var result = ColorMapper.Apply(intensities, rgb, ColorMap.Grayscale);

        Assert.True(result.IsFailure);
        Assert.Equal("imaging.rgb_buffer_length_mismatch", result.Error.Code);
    }
}
