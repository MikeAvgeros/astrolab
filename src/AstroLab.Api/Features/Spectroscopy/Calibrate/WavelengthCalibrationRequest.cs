using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Spectroscopy.Calibrate;

public sealed record WavelengthCalibrationRequest
{
    [JsonConstructor]
    private WavelengthCalibrationRequest(ImmutableList<double> pixelPositions, ImmutableList<double> knownWavelengths)
    {
        PixelPositions = pixelPositions;
        KnownWavelengths = knownWavelengths;
    }

    public ImmutableList<double> PixelPositions { get; }

    public ImmutableList<double> KnownWavelengths { get; }

    public static WavelengthCalibrationRequest Create(ImmutableList<double> pixelPositions, ImmutableList<double> knownWavelengths) =>
        new(pixelPositions, knownWavelengths);
}
