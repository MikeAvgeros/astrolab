namespace AstroLab.Api.Features.Catalogues.CrossMatch;

public sealed record CrossMatchResponse(IReadOnlyList<CrossMatchEntryDto> Matches);
