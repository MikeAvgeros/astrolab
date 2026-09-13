using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits.Observation;

public sealed record ObservationHeaderMetadataDto
{
    private ObservationHeaderMetadataDto(
        string? @object, string? dateObs, string? telescope, string? instrument, string? observer,
        double? exposureTimeSeconds, string? filter, string? rightAscension, string? declination, double? equinox, string? bunit,
        int? detectorWidth, int? detectorHeight, double? gainElectronsPerAdu, double? focalLengthMillimeters,
        string? originOrganization, string? archiveFilename, string? checksum, string? dataSum)
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
        DetectorWidth = detectorWidth;
        DetectorHeight = detectorHeight;
        GainElectronsPerAdu = gainElectronsPerAdu;
        FocalLengthMillimeters = focalLengthMillimeters;
        OriginOrganization = originOrganization;
        ArchiveFilename = archiveFilename;
        Checksum = checksum;
        DataSum = dataSum;
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

    public int? DetectorWidth { get; }

    public int? DetectorHeight { get; }

    public double? GainElectronsPerAdu { get; }

    public double? FocalLengthMillimeters { get; }

    public string? OriginOrganization { get; }

    public string? ArchiveFilename { get; }

    public string? Checksum { get; }

    public string? DataSum { get; }

    public static ObservationHeaderMetadataDto Create(FitsHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);

        return new ObservationHeaderMetadataDto(
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
            bunit: AsText(header, "BUNIT"),
            detectorWidth: AsInteger(header, "NAXIS1"),
            detectorHeight: AsInteger(header, "NAXIS2"),
            gainElectronsPerAdu: AsNumber(header, "GAIN"),
            focalLengthMillimeters: AsNumber(header, "FOCALLEN"),
            originOrganization: AsText(header, "ORIGIN"),
            archiveFilename: AsText(header, "ARCFILE"),
            checksum: AsText(header, "CHECKSUM"),
            dataSum: AsText(header, "DATASUM"));
    }

    private static string? AsText(FitsHeader header, string keyword) =>
        header.TryGetValue(keyword, out var value) ? value.ToString() : null;

    private static double? AsNumber(FitsHeader header, string keyword)
    {
        var result = header.GetReal(keyword);

        return result.IsSuccess ? result.Value : null;
    }

    private static int? AsInteger(FitsHeader header, string keyword)
    {
        var result = header.GetInteger(keyword);

        return result.IsSuccess ? (int)result.Value : null;
    }
}
