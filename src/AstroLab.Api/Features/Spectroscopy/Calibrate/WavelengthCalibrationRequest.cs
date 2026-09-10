using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Spectroscopy.Calibrate;

public sealed record WavelengthCalibrationRequest
{
    [JsonConstructor]
    private WavelengthCalibrationRequest(
        ImmutableList<double> pixelPositions, ImmutableList<double> knownWavelengths, ImmutableList<double>? fluxSensitivity)
    {
        PixelPositions = pixelPositions;
        KnownWavelengths = knownWavelengths;
        FluxSensitivity = fluxSensitivity;
    }

    public ImmutableList<double> PixelPositions { get; }

    public ImmutableList<double> KnownWavelengths { get; }
    
    public ImmutableList<double>? FluxSensitivity { get; }

    public static WavelengthCalibrationRequest Create(
        ImmutableList<double> pixelPositions, ImmutableList<double> knownWavelengths, ImmutableList<double>? fluxSensitivity = null) =>
        new(pixelPositions, knownWavelengths, fluxSensitivity);
}
