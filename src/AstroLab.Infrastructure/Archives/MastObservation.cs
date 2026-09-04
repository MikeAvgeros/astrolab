namespace AstroLab.Infrastructure.Archives;

internal sealed record MastObservation
{
    private MastObservation(
        string obsId, string? targetName, string? collection, string? instrument, string? dataProductType,
        int? calibrationLevel, double? observationStart, double? observationEnd, double? exposureTime,
        double? rightAscension, double? declination, double? wavelengthMin, double? wavelengthMax,
        string? proposalId, string? proposalPi, string? dataRights)
    {
        ObsId = obsId;
        TargetName = targetName;
        Collection = collection;
        Instrument = instrument;
        DataProductType = dataProductType;
        CalibrationLevel = calibrationLevel;
        ObservationStart = observationStart;
        ObservationEnd = observationEnd;
        ExposureTime = exposureTime;
        RightAscension = rightAscension;
        Declination = declination;
        WavelengthMin = wavelengthMin;
        WavelengthMax = wavelengthMax;
        ProposalId = proposalId;
        ProposalPi = proposalPi;
        DataRights = dataRights;
    }

    public string ObsId { get; }

    public string? TargetName { get; }

    public string? Collection { get; }

    public string? Instrument { get; }

    public string? DataProductType { get; }

    public int? CalibrationLevel { get; }

    public double? ObservationStart { get; }

    public double? ObservationEnd { get; }

    public double? ExposureTime { get; }

    public double? RightAscension { get; }

    public double? Declination { get; }

    public double? WavelengthMin { get; }

    public double? WavelengthMax { get; }

    public string? ProposalId { get; }

    public string? ProposalPi { get; }

    public string? DataRights { get; }

    public static MastObservation Create(
        string obsId, string? targetName, string? collection, string? instrument, string? dataProductType,
        int? calibrationLevel, double? observationStart, double? observationEnd, double? exposureTime,
        double? rightAscension, double? declination, double? wavelengthMin, double? wavelengthMax,
        string? proposalId, string? proposalPi, string? dataRights)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(obsId);

        return new MastObservation(
            obsId, targetName, collection, instrument, dataProductType, calibrationLevel, observationStart,
            observationEnd, exposureTime, rightAscension, declination, wavelengthMin, wavelengthMax,
            proposalId, proposalPi, dataRights);
    }
}
