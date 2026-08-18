namespace AstroLab.Api.Features.Spectroscopy.Calibrate;

public sealed record WavelengthCalibrationRequest(double[] PixelPositions, double[] KnownWavelengths);
