namespace AstroLab.Api.Features.Spectroscopy.Calibrate;

public sealed record WavelengthCalibrationResponse(string FileId, double[] DispersionCoefficients, double ResidualRms);
