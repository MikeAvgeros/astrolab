using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits.Inspect;

public sealed record FitsHduSummaryDto(int Index, HduType Type, IReadOnlyList<int> NAxes);
