using System.Collections.Immutable;

namespace AstroLab.Api.Features.Catalogues.CrossMatch;

public sealed record CrossMatchResponse(ImmutableList<CrossMatchEntryDto> Matches);

/// <summary>Static factory accompanying <see cref="CrossMatchResponse"/>.</summary>
public static class CrossMatchResponseFactory
{
    public static CrossMatchResponse Create(ImmutableList<CrossMatchEntryDto> matches) =>
        new(matches);
}
