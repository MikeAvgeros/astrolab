using System.Collections.Immutable;

namespace AstroLab.Api.Features.Catalogues.CrossMatch;

public sealed record CrossMatchResponse
{
    private CrossMatchResponse(ImmutableList<CrossMatchEntryDto> matches)
    {
        Matches = matches;
    }

    public ImmutableList<CrossMatchEntryDto> Matches { get; }

    public static CrossMatchResponse Create(ImmutableList<CrossMatchEntryDto> matches) =>
        new(matches);
}
