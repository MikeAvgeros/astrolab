using System.Collections.Immutable;

namespace AstroLab.Api.Features.Spectroscopy.ContinuumSubtract;

public sealed record ContinuumSubtractResponse
{
    private ContinuumSubtractResponse(
        string fileId, ImmutableList<double> wavelengths, ImmutableList<double> flux, ImmutableList<double> continuumSubtractedFlux)
    {
        FileId = fileId;
        Wavelengths = wavelengths;
        Flux = flux;
        ContinuumSubtractedFlux = continuumSubtractedFlux;
    }

    public string FileId { get; }

    public ImmutableList<double> Wavelengths { get; }

    public ImmutableList<double> Flux { get; }

    public ImmutableList<double> ContinuumSubtractedFlux { get; }

    public static ContinuumSubtractResponse Create(
        string fileId, ImmutableList<double> wavelengths, ImmutableList<double> flux, ImmutableList<double> continuumSubtractedFlux)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new ContinuumSubtractResponse(fileId, wavelengths, flux, continuumSubtractedFlux);
    }
}
