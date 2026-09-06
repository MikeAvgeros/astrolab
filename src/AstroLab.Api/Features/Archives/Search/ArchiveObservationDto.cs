using AstroLab.Infrastructure.Archives;

namespace AstroLab.Api.Features.Archives.Search;

public sealed record ArchiveObservationDto
{
    private ArchiveObservationDto(
        string datasetId, string target, string instrument, DateTimeOffset observationDate, ArchiveSource source,
        string? collection, string? dataProductType, int? calibrationLevel,
        double? rightAscension, double? declination, double? exposureTimeSeconds,
        double? wavelengthMinMicrometres, double? wavelengthMaxMicrometres,
        string? proposalId, string? proposalPi, string? dataRights)
    {
        DatasetId = datasetId;
        Target = target;
        Instrument = instrument;
        ObservationDate = observationDate;
        Source = source;
        Collection = collection;
        DataProductType = dataProductType;
        CalibrationLevel = calibrationLevel;
        RightAscension = rightAscension;
        Declination = declination;
        ExposureTimeSeconds = exposureTimeSeconds;
        WavelengthMinMicrometres = wavelengthMinMicrometres;
        WavelengthMaxMicrometres = wavelengthMaxMicrometres;
        ProposalId = proposalId;
        ProposalPi = proposalPi;
        DataRights = dataRights;
    }

    public string DatasetId { get; }

    public string Target { get; }

    public string Instrument { get; }

    public DateTimeOffset ObservationDate { get; }

    public ArchiveSource Source { get; }

    public string? Collection { get; }

    public string? DataProductType { get; }

    public int? CalibrationLevel { get; }

    public double? RightAscension { get; }

    public double? Declination { get; }

    public double? ExposureTimeSeconds { get; }

    public double? WavelengthMinMicrometres { get; }

    public double? WavelengthMaxMicrometres { get; }

    public string? ProposalId { get; }

    public string? ProposalPi { get; }

    public string? DataRights { get; }

    public static ArchiveObservationDto Create(ArchiveObservation observation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(observation.DatasetId);
        
        ArgumentException.ThrowIfNullOrWhiteSpace(observation.Target);
        
        ArgumentException.ThrowIfNullOrWhiteSpace(observation.Instrument);

        return new ArchiveObservationDto(
            observation.DatasetId,
            observation.Target,
            observation.Instrument,
            observation.ObservationDate,
            observation.Source,
            observation.Collection,
            observation.DataProductType,
            observation.CalibrationLevel,
            observation.RightAscension,
            observation.Declination,
            observation.ExposureTimeSeconds,
            observation.WavelengthMinMicrometres,
            observation.WavelengthMaxMicrometres,
            observation.ProposalId,
            observation.ProposalPi,
            observation.DataRights);
    }
}
