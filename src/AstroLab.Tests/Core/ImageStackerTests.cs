using AstroLab.Core.Imaging;

namespace AstroLab.Tests.Core;

public class ImageStackerTests
{
    private static ReadOnlyMemory<float> Frame(params float[] pixels) => pixels;

    [Fact]
    public void Combine_Mean_AveragesEachPixelAcrossFrames()
    {
        List<ReadOnlyMemory<float>> frames = [Frame(10f, 20f), Frame(20f, 40f), Frame(30f, 60f)];

        var result = ImageStacker.Combine(frames, width: 2, height: 1, StackCombinationMethod.Mean);

        Assert.True(result.IsSuccess);

        Assert.Equal([20f, 40f], result.Value);
    }

    [Fact]
    public void Combine_MedianWithOddFrameCount_ReturnsMiddleValue()
    {
        List<ReadOnlyMemory<float>> frames = [Frame(1f), Frame(100f), Frame(5f)];

        var result = ImageStacker.Combine(frames, width: 1, height: 1, StackCombinationMethod.Median);

        Assert.True(result.IsSuccess);

        Assert.Equal(5f, result.Value[0]);
    }

    [Fact]
    public void Combine_MedianWithEvenFrameCount_AveragesTwoMiddleValues()
    {
        List<ReadOnlyMemory<float>> frames = [Frame(1f), Frame(2f), Frame(3f), Frame(4f)];

        var result = ImageStacker.Combine(frames, width: 1, height: 1, StackCombinationMethod.Median);

        Assert.True(result.IsSuccess);

        Assert.Equal(2.5f, result.Value[0]);
    }

    [Fact]
    public void Combine_Sum_AddsEachPixelAcrossFrames()
    {
        List<ReadOnlyMemory<float>> frames = [Frame(1f), Frame(2f), Frame(3f)];

        var result = ImageStacker.Combine(frames, width: 1, height: 1, StackCombinationMethod.Sum);

        Assert.True(result.IsSuccess);

        Assert.Equal(6f, result.Value[0]);
    }

    [Fact]
    public void Combine_SigmaClipped_RejectsOutlierAndReturnsMeanOfSurvivors()
    {
        // A tighter-than-default threshold avoids the "masking" effect where one huge outlier among few samples inflates sigma enough to hide itself.
        List<ReadOnlyMemory<float>> frames = [Frame(10f), Frame(10f), Frame(10f), Frame(10f), Frame(1000f)];

        var result = ImageStacker.Combine(frames, width: 1, height: 1, StackCombinationMethod.SigmaClipped, sigmaClipThreshold: 1.0);

        Assert.True(result.IsSuccess);

        Assert.Equal(10.0, result.Value[0], precision: 3);
    }

    [Fact]
    public void Combine_WhenAllFramesNonFiniteAtAPixel_ProducesNaNForThatPixel()
    {
        List<ReadOnlyMemory<float>> frames = [Frame(float.NaN), Frame(float.NaN)];

        var result = ImageStacker.Combine(frames, width: 1, height: 1, StackCombinationMethod.Mean);

        Assert.True(result.IsSuccess);

        Assert.True(float.IsNaN(result.Value[0]));
    }

    [Fact]
    public void Combine_SkipsNonFiniteFrameContributionsButKeepsOthers()
    {
        List<ReadOnlyMemory<float>> frames = [Frame(10f), Frame(float.NaN), Frame(20f)];

        var result = ImageStacker.Combine(frames, width: 1, height: 1, StackCombinationMethod.Mean);

        Assert.True(result.IsSuccess);

        Assert.Equal(15f, result.Value[0]);
    }

    [Fact]
    public void Combine_OnMismatchedFrameDimensions_ReturnsValidationError()
    {
        List<ReadOnlyMemory<float>> frames = [Frame(1f, 2f), Frame(1f, 2f, 3f)];

        var result = ImageStacker.Combine(frames, width: 2, height: 1, StackCombinationMethod.Mean);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.stack.dimension_mismatch", result.Error.Code);
    }

    [Fact]
    public void Combine_OnNoFrames_ReturnsValidationError()
    {
        var result = ImageStacker.Combine([], width: 2, height: 1, StackCombinationMethod.Mean);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.stack.no_frames", result.Error.Code);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Combine_OnInvalidSigmaClipThreshold_ReturnsValidationError(double threshold)
    {
        List<ReadOnlyMemory<float>> frames = [Frame(1f), Frame(2f)];

        var result = ImageStacker.Combine(frames, width: 1, height: 1, StackCombinationMethod.SigmaClipped, sigmaClipThreshold: threshold);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.stack.invalid_sigma_clip_threshold", result.Error.Code);
    }

    [Fact]
    public void Combine_OnInvalidSigmaClipIterations_ReturnsValidationError()
    {
        List<ReadOnlyMemory<float>> frames = [Frame(1f), Frame(2f)];

        var result = ImageStacker.Combine(frames, width: 1, height: 1, StackCombinationMethod.SigmaClipped, sigmaClipIterations: 0);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.stack.invalid_sigma_clip_iterations", result.Error.Code);
    }
}
