using System.Collections.Immutable;
using AstroLab.Core.Catalogues;

namespace AstroLab.Api.Features.Catalogues.CrossMatch;

public sealed record CrossMatchResponse
{
    private CrossMatchResponse(string fileId, ImmutableList<CrossMatchEntryDto> matches)
    {
        FileId = fileId;
        Matches = matches;
    }

    public string FileId { get; }

    public ImmutableList<CrossMatchEntryDto> Matches { get; }

    public static CrossMatchResponse Create(string fileId, IReadOnlyList<CatalogueMatch> matches)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new CrossMatchResponse(fileId, [.. matches.Select(CrossMatchEntryDto.Create)]);
    }
}
