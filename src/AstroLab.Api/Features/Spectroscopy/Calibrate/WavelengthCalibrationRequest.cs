using System.Collections.Immutable;

namespace AstroLab.Api.Features.Spectroscopy.Calibrate;

public sealed record WavelengthCalibrationRequest(ImmutableList<double> PixelPositions, ImmutableList<double> KnownWavelengths);

/// <summary>Static factory accompanying <see cref="WavelengthCalibrationRequest"/>. Validates arguments before constructing.</summary>
public static class WavelengthCalibrationRequestFactory
{
    public static WavelengthCalibrationRequest Create(ImmutableList<double> pixelPositions, ImmutableList<double> knownWavelengths) =>
        new(pixelPositions, knownWavelengths);
}
