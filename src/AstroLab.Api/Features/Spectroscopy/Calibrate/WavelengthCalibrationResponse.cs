using System.Collections.Immutable;

namespace AstroLab.Api.Features.Spectroscopy.Calibrate;

public sealed record WavelengthCalibrationResponse
{
    private WavelengthCalibrationResponse(
        string fileId,
        ImmutableList<double> dispersionCoefficients,
        double residualRms,
        ImmutableList<double> wavelengths,
        ImmutableList<double> flux,
        bool fluxCalibrated)
    {
        FileId = fileId;
        DispersionCoefficients = dispersionCoefficients;
        ResidualRms = residualRms;
        Wavelengths = wavelengths;
        Flux = flux;
        FluxCalibrated = fluxCalibrated;
    }

    public string FileId { get; }

    public ImmutableList<double> DispersionCoefficients { get; }

    public double ResidualRms { get; }
    
    public ImmutableList<double> Wavelengths { get; }

    public ImmutableList<double> Flux { get; }
    
    public bool FluxCalibrated { get; }

    public static WavelengthCalibrationResponse Create(
        string fileId,
        ImmutableList<double> dispersionCoefficients,
        double residualRms,
        ImmutableList<double> wavelengths,
        ImmutableList<double> flux,
        bool fluxCalibrated)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new WavelengthCalibrationResponse(fileId, dispersionCoefficients, residualRms, wavelengths, flux, fluxCalibrated);
    }
}
