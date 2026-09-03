using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits.Inspect;

public sealed record FitsCommonMetadataDto
{
    private FitsCommonMetadataDto(
        string? @object,
        string? dateObs,
        string? telescope,
        string? instrument,
        string? observer,
        double? exposureTimeSeconds,
        string? filter,
        string? rightAscension,
        string? declination,
        double? equinox,
        string? bunit)
    {
        Object = @object;
        DateObs = dateObs;
        Telescope = telescope;
        Instrument = instrument;
        Observer = observer;
        ExposureTimeSeconds = exposureTimeSeconds;
        Filter = filter;
        RightAscension = rightAscension;
        Declination = declination;
        Equinox = equinox;
        Bunit = bunit;
    }

    public string? Object { get; }

    public string? DateObs { get; }

    public string? Telescope { get; }

    public string? Instrument { get; }

    public string? Observer { get; }

    public double? ExposureTimeSeconds { get; }

    public string? Filter { get; }

    public string? RightAscension { get; }

    public string? Declination { get; }

    public double? Equinox { get; }

    public string? Bunit { get; }

    public static FitsCommonMetadataDto Create(
        string? @object,
        string? dateObs,
        string? telescope,
        string? instrument,
        string? observer,
        double? exposureTimeSeconds,
        string? filter,
        string? rightAscension,
        string? declination,
        double? equinox,
        string? bunit) =>
        new(@object, dateObs, telescope, instrument, observer, exposureTimeSeconds, filter, rightAscension, declination, equinox, bunit);

    public static FitsCommonMetadataDto FromHeader(FitsHeader header) => Create(
        @object: AsText(header, "OBJECT"),
        dateObs: AsText(header, "DATE-OBS"),
        telescope: AsText(header, "TELESCOP"),
        instrument: AsText(header, "INSTRUME"),
        observer: AsText(header, "OBSERVER"),
        exposureTimeSeconds: AsNumber(header, "EXPTIME"),
        filter: AsText(header, "FILTER"),
        rightAscension: AsText(header, "RA"),
        declination: AsText(header, "DEC"),
        equinox: AsNumber(header, "EQUINOX"),
        bunit: AsText(header, "BUNIT"));

    private static string? AsText(FitsHeader header, string keyword) =>
        header.TryGetValue(keyword, out var value) ? value.ToString() : null;

    private static double? AsNumber(FitsHeader header, string keyword)
    {
        var result = header.GetReal(keyword);

        return result.IsSuccess ? result.Value : null;
    }
}
