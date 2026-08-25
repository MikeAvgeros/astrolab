using System.Collections.Immutable;

namespace AstroLab.Api.Features.Spectroscopy.Calibrate;

public sealed record WavelengthCalibrationResponse(string FileId, ImmutableList<double> DispersionCoefficients, double ResidualRms);

/// <summary>Static factory accompanying <see cref="WavelengthCalibrationResponse"/>. Validates arguments before constructing.</summary>
public static class WavelengthCalibrationResponseFactory
{
    public static WavelengthCalibrationResponse Create(string fileId, ImmutableList<double> dispersionCoefficients, double residualRms)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new WavelengthCalibrationResponse(fileId, dispersionCoefficients, residualRms);
    }
}
