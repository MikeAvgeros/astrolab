using System.Collections.Immutable;

namespace AstroLab.Api.Features.Spectroscopy.Continuum;

public sealed record ContinuumResponse
{
    private ContinuumResponse(
        string fileId,
        ImmutableList<double> wavelengths,
        ImmutableList<double> flux,
        ImmutableList<double> continuum,
        ImmutableList<double> coefficients)
    {
        FileId = fileId;
        Wavelengths = wavelengths;
        Flux = flux;
        Continuum = continuum;
        Coefficients = coefficients;
    }

    public string FileId { get; }

    public ImmutableList<double> Wavelengths { get; }

    public ImmutableList<double> Flux { get; }

    public ImmutableList<double> Continuum { get; }

    public ImmutableList<double> Coefficients { get; }

    public static ContinuumResponse Create(
        string fileId,
        ImmutableList<double> wavelengths,
        ImmutableList<double> flux,
        ImmutableList<double> continuum,
        ImmutableList<double> coefficients)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new ContinuumResponse(fileId, wavelengths, flux, continuum, coefficients);
    }
}
