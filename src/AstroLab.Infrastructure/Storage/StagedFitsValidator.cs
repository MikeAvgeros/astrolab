using AstroLab.Core.Fits;
using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// Validates a file that has just been streamed into staging storage (a user upload or an archive
/// download) and deletes it if it is not a conforming FITS file, so invalid bytes never remain
/// staged. Only the headers are read — data units are skipped with a seek — so validating a large
/// file never buffers its pixel data.
/// </summary>
public sealed class StagedFitsValidator
{
    private readonly ILocalFileStore _fileStore;
    private readonly FitsDatasetReader _datasetReader;

    public StagedFitsValidator(ILocalFileStore fileStore, FitsDatasetReader datasetReader)
    {
        _fileStore = fileStore;
        _datasetReader = datasetReader;
    }
    
    public async Task<Result<Unit>> ValidateOrDiscardAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        Result<Unit> validationResult;

        try
        {
            var hdusResult = await _datasetReader.ReadAllHdusAsync(relativeKey, cancellationToken);

            validationResult = hdusResult.Bind(hdus => FitsConformance.ValidatePrimaryHeader(hdus[0].Header));
        }
        catch (OperationCanceledException)
        {
            _ = _fileStore.Delete(relativeKey);

            throw;
        }

        if (validationResult.IsFailure)
        {
            _ = _fileStore.Delete(relativeKey);
        }

        return validationResult;
    }
}
