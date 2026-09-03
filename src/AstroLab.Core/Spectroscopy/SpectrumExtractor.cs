using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Pure one-dimensional spectral extraction algorithms for long-slit/fiber spectroscopy: boxcar
/// aperture extraction along a (possibly curved) trace, background subtraction, and polynomial
/// wavelength calibration. Operates directly over spans with no I/O and no large intermediate
/// allocations.
/// </summary>
public static class SpectrumExtractor
{
    public static Result<Unit> ExtractBoxcar(
        ReadOnlySpan<float> image,
        int width,
        int height,
        DispersionAxis axis,
        ReadOnlySpan<double> traceCenters,
        double apertureHalfWidth,
        Span<double> spectrum)
    {
        if (width <= 0 || height <= 0 || image.Length != width * height)
        {
            return Error.Validation(
                "spectroscopy.invalid_image_bounds",
                $"Image span length ({image.Length}) does not match width x height ({width}x{height}).");
        }

        var dispersionBins = axis == DispersionAxis.Horizontal ? width : height;

        var spatialExtent = axis == DispersionAxis.Horizontal ? height : width;

        if (traceCenters.Length != dispersionBins)
        {
            return Error.Validation(
                "spectroscopy.trace_length_mismatch",
                $"traceCenters length ({traceCenters.Length}) must equal the dispersion axis extent ({dispersionBins}).");
        }

        if (spectrum.Length != dispersionBins)
        {
            return Error.Validation(
                "spectroscopy.output_length_mismatch",
                $"spectrum length ({spectrum.Length}) must equal the dispersion axis extent ({dispersionBins}).");
        }

        if (apertureHalfWidth <= 0 || !double.IsFinite(apertureHalfWidth))
        {
            return Error.Validation("spectroscopy.invalid_aperture",
                "apertureHalfWidth must be a finite, positive value.");
        }

        for (var d = 0; d < dispersionBins; d++)
        {
            var center = traceCenters[d];

            var lowerEdge = center - apertureHalfWidth;

            var upperEdge = center + apertureHalfWidth;

            var sMin = Math.Max(0, (int)Math.Floor(lowerEdge));

            var sMax = Math.Min(spatialExtent - 1, (int)Math.Ceiling(upperEdge) - 1);

            double flux = 0.0;

            for (var s = sMin; s <= sMax; s++)
            {
                var overlap = Math.Min(s + 1, upperEdge) - Math.Max(s, lowerEdge);

                if (overlap <= 0)
                {
                    continue;
                }

                var value = image[axis == DispersionAxis.Horizontal ? (s * width) + d : (d * width) + s];

                if (!float.IsFinite(value))
                {
                    continue;
                }

                flux += value * overlap;
            }

            spectrum[d] = flux;
        }

        return Result<Unit>.Success(Unit.Value);
    }

    public static Result<Unit> SubtractBackground(Span<double> spectrum, ReadOnlySpan<double> background)
    {
        if (spectrum.Length != background.Length)
        {
            return Error.Validation(
                "spectroscopy.background_length_mismatch",
                $"background length ({background.Length}) must equal spectrum length ({spectrum.Length}).");
        }

        for (var i = 0; i < spectrum.Length; i++)
        {
            spectrum[i] -= background[i];
        }

        return Result<Unit>.Success(Unit.Value);
    }

    public static double EvaluateWavelength(double pixelIndex, ReadOnlySpan<double> dispersionCoefficients)
    {
        var result = 0.0;

        for (var i = dispersionCoefficients.Length - 1; i >= 0; i--)
        {
            result = result * pixelIndex + dispersionCoefficients[i];
        }

        return result;
    }

    public static Result<Unit> ComputeWavelengths(
        ReadOnlySpan<double> pixelIndices, ReadOnlySpan<double> dispersionCoefficients, Span<double> wavelengths)
    {
        if (pixelIndices.Length != wavelengths.Length)
        {
            return Error.Validation(
                "spectroscopy.wavelength_length_mismatch",
                $"wavelengths length ({wavelengths.Length}) must equal pixelIndices length ({pixelIndices.Length}).");
        }

        if (dispersionCoefficients.IsEmpty)
        {
            return Error.Validation("spectroscopy.empty_dispersion_solution",
                "dispersionCoefficients must contain at least one coefficient.");
        }

        for (var i = 0; i < pixelIndices.Length; i++)
        {
            wavelengths[i] = EvaluateWavelength(pixelIndices[i], dispersionCoefficients);
        }

        return Result<Unit>.Success(Unit.Value);
    }
}
