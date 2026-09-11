using System.Collections.Immutable;
using AstroLab.Core.Astrometry;
using AstroLab.Core.Result;

namespace AstroLab.Core.Catalogues;

/// <summary>
/// Pure nearest-neighbour cross-match: pairs each detected image source with the closest
/// catalogue candidate within <paramref name="radiusArcsec"/> of it, using the same great-circle
/// separation as <see cref="AngularSeparation"/>. A source with no candidate inside the radius is
/// simply omitted from the result rather than reported with a null match.
/// </summary>
public static class CatalogueCrossMatcher
{
    public static Result<ImmutableList<CatalogueMatch>> Match(
        IReadOnlyList<(int SourceId, double RightAscension, double Declination)> sources,
        IReadOnlyList<CatalogueMatchCandidate> candidates,
        double radiusArcsec)
    {
        if (radiusArcsec <= 0)
        {
            return Error.Validation("catalogues.crossmatch.invalid_radius", "radiusArcsec must be positive.");
        }

        var builder = ImmutableList.CreateBuilder<CatalogueMatch>();

        foreach (var source in sources)
        {
            CatalogueMatchCandidate? bestCandidate = null;

            var bestSeparationArcsec = double.PositiveInfinity;

            foreach (var candidate in candidates)
            {
                var separationResult = AngularSeparation.ComputeArcseconds(
                    source.RightAscension, source.Declination, candidate.RightAscension, candidate.Declination);

                if (separationResult.IsFailure)
                {
                    continue;
                }

                if (separationResult.Value <= radiusArcsec && separationResult.Value < bestSeparationArcsec)
                {
                    bestCandidate = candidate;
                    bestSeparationArcsec = separationResult.Value;
                }
            }

            if (bestCandidate is { } matchedCandidate)
            {
                builder.Add(CatalogueMatch.Create(source.SourceId, matchedCandidate, bestSeparationArcsec));
            }
        }

        return builder.ToImmutable();
    }
}
