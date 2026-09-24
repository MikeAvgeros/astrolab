using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

/// <summary>
/// Combines multiple equally-sized, pre-aligned pixel frames into a single composite image via
/// mean, median, sum, or iterative sigma-clipped mean combination — the standard astronomical
/// stacking techniques for improving signal-to-noise and rejecting cosmic rays/outliers. Sigma
/// clipping is centred on the per-pixel median and scaled by the median absolute deviation
/// (1.4826·MAD), so a single outlier cannot inflate the dispersion estimate enough to hide itself —
/// with a mean/standard-deviation clip no sample of an n-frame stack can lie more than √(n−1)
/// standard deviations from the mean, which makes a 3σ clip a no-op for ten frames or fewer.
/// </summary>
public static class ImageStacker
{
    public const double DefaultSigmaClipThreshold = 3.0;
    public const int DefaultSigmaClipIterations = 3;
    private const int MaxStackallocFrameCount = 64;
    private const int MinimumSigmaClipSampleCount = 3;
    private const double MadToSigmaFactor = 1.4826;

    public static Result<float[]> Combine(
        IReadOnlyList<ReadOnlyMemory<float>> frames,
        int width,
        int height,
        StackCombinationMethod method,
        double sigmaClipThreshold = DefaultSigmaClipThreshold,
        int sigmaClipIterations = DefaultSigmaClipIterations)
    {
        if (width <= 0 || height <= 0)
        {
            return Error.Validation("imaging.stack.invalid_image_bounds", $"width and height must be positive (got {width}x{height}).");
        }

        if (frames.Count == 0)
        {
            return Error.Validation("imaging.stack.no_frames", "At least one frame is required to stack.");
        }

        var pixelCount = width * height;

        foreach (var frame in frames)
        {
            if (frame.Length != pixelCount)
            {
                return Error.Validation(
                    "imaging.stack.dimension_mismatch",
                    $"Every frame must contain exactly {pixelCount} pixels ({width}x{height}); found a frame with {frame.Length}.");
            }
        }

        if (!Enum.IsDefined(method))
        {
            return Error.Validation("imaging.stack.invalid_method", $"Unsupported stack combination method: {method}.");
        }

        if (sigmaClipThreshold <= 0.0 || !double.IsFinite(sigmaClipThreshold))
        {
            return Error.Validation("imaging.stack.invalid_sigma_clip_threshold", "sigmaClipThreshold must be a finite, positive value.");
        }

        if (sigmaClipIterations < 1)
        {
            return Error.Validation("imaging.stack.invalid_sigma_clip_iterations", "sigmaClipIterations must be at least 1.");
        }

        var frameCount = frames.Count;

        var result = new float[pixelCount];

        Span<double> sampleBuffer = frameCount <= MaxStackallocFrameCount ? stackalloc double[frameCount] : new double[frameCount];

        Span<double> deviationBuffer = frameCount <= MaxStackallocFrameCount ? stackalloc double[frameCount] : new double[frameCount];

        for (var pixelIndex = 0; pixelIndex < pixelCount; pixelIndex++)
        {
            var sampleCount = CollectFiniteSamples(frames, pixelIndex, sampleBuffer);

            result[pixelIndex] = sampleCount == 0
                ? float.NaN
                : (float)CombineSamples(sampleBuffer[..sampleCount], deviationBuffer, method, sigmaClipThreshold, sigmaClipIterations);
        }

        return result;
    }

    private static int CollectFiniteSamples(IReadOnlyList<ReadOnlyMemory<float>> frames, int pixelIndex, Span<double> buffer)
    {
        var count = 0;

        foreach (var frame in frames)
        {
            var value = frame.Span[pixelIndex];

            if (float.IsFinite(value))
            {
                buffer[count++] = value;
            }
        }

        return count;
    }

    private static double CombineSamples(
        Span<double> samples, Span<double> deviationBuffer, StackCombinationMethod method, double sigmaClipThreshold, int sigmaClipIterations) => method switch
    {
        StackCombinationMethod.Mean => Mean(samples),
        StackCombinationMethod.Median => Median(samples),
        StackCombinationMethod.Sum => Sum(samples),
        StackCombinationMethod.SigmaClipped => SigmaClippedMean(samples, deviationBuffer, sigmaClipThreshold, sigmaClipIterations),
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported stack combination method."),
    };

    private static double Sum(ReadOnlySpan<double> samples)
    {
        var sum = 0.0;

        foreach (var sample in samples)
        {
            sum += sample;
        }

        return sum;
    }

    private static double Mean(ReadOnlySpan<double> samples) => Sum(samples) / samples.Length;

    private static double Median(Span<double> samples)
    {
        samples.Sort();

        var midpoint = samples.Length / 2;

        return samples.Length % 2 == 0 ? (samples[midpoint - 1] + samples[midpoint]) / 2.0 : samples[midpoint];
    }

    private static double SigmaClippedMean(Span<double> samples, Span<double> deviationBuffer, double threshold, int iterations)
    {
        var activeCount = samples.Length;

        for (var iteration = 0; iteration < iterations && activeCount >= MinimumSigmaClipSampleCount; iteration++)
        {
            var active = samples[..activeCount];

            var center = Median(active);

            var deviations = deviationBuffer[..activeCount];

            for (var i = 0; i < activeCount; i++)
            {
                deviations[i] = Math.Abs(active[i] - center);
            }

            var sigma = MadToSigmaFactor * Median(deviations);

            var survivorCount = 0;

            for (var i = 0; i < activeCount; i++)
            {
                if (Math.Abs(active[i] - center) <= threshold * sigma)
                {
                    active[survivorCount++] = active[i];
                }
            }

            if (survivorCount == activeCount || survivorCount == 0)
            {
                break;
            }

            activeCount = survivorCount;
        }

        return Mean(samples[..activeCount]);
    }
}
