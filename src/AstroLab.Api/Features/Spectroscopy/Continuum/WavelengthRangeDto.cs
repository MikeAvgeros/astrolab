namespace AstroLab.Api.Features.Spectroscopy.Continuum;

public sealed record WavelengthRangeDto
{
    private WavelengthRangeDto(double minWavelength, double maxWavelength)
    {
        MinWavelength = minWavelength;
        MaxWavelength = maxWavelength;
    }

    public double MinWavelength { get; }

    public double MaxWavelength { get; }

    public static WavelengthRangeDto Create(double minWavelength, double maxWavelength) =>
        new(minWavelength, maxWavelength);
}
