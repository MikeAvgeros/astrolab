using System.Collections.Immutable;

namespace AstroLab.Api.Features.Spectroscopy.Calibrate;

public sealed record WavelengthCalibrationResponse
{
    private WavelengthCalibrationResponse(string fileId, ImmutableList<double> dispersionCoefficients, double residualRms)
    {
        FileId = fileId;
        DispersionCoefficients = dispersionCoefficients;
        ResidualRms = residualRms;
    }

    public string FileId { get; }

    public ImmutableList<double> DispersionCoefficients { get; }

    public double ResidualRms { get; }

    public static WavelengthCalibrationResponse Create(string fileId, ImmutableList<double> dispersionCoefficients, double residualRms)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new WavelengthCalibrationResponse(fileId, dispersionCoefficients, residualRms);
    }
}
