using AstroLab.Core.Catalogues;

namespace AstroLab.Tests.Core;

public class CatalogueCrossMatcherTests
{
    private const double ArcsecPerDegree = 3600.0;

    [Fact]
    public void Match_CandidateWithinRadius_ReturnsMatchWithSeparation()
    {
        var sources = new[] { (SourceId: 1, RightAscension: 180.0, Declination: 0.0) };

        var candidate = CatalogueMatchCandidate.Create("I/355/gaiadr3", "Gaia DR3 1", 180.0, 0.0002, magnitude: 15.2);

        var result = CatalogueCrossMatcher.Match(sources, [candidate], radiusArcsec: 5.0);

        Assert.True(result.IsSuccess);

        var match = Assert.Single(result.Value);

        Assert.Equal(1, match.SourceId);

        Assert.Equal("Gaia DR3 1", match.Candidate.Identifier);

        Assert.Equal(0.72, match.SeparationArcsec, precision: 1);
    }

    [Fact]
    public void Match_NoCandidateWithinRadius_OmitsSource()
    {
        var sources = new[] { (SourceId: 1, RightAscension: 180.0, Declination: 0.0) };

        var farCandidate = CatalogueMatchCandidate.Create("I/355/gaiadr3", "Gaia DR3 2", 181.0, 0.0);

        var result = CatalogueCrossMatcher.Match(sources, [farCandidate], radiusArcsec: 5.0);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void Match_MultipleCandidates_PicksNearestWithinRadius()
    {
        var sources = new[] { (SourceId: 1, RightAscension: 180.0, Declination: 0.0) };

        var nearCandidate = CatalogueMatchCandidate.Create("cat", "near", 180.0, 1.0 / ArcsecPerDegree);

        var farCandidate = CatalogueMatchCandidate.Create("cat", "far", 180.0, 3.0 / ArcsecPerDegree);

        var result = CatalogueCrossMatcher.Match(sources, [farCandidate, nearCandidate], radiusArcsec: 5.0);

        var match = Assert.Single(result.Value);

        Assert.Equal("near", match.Candidate.Identifier);
    }

    [Fact]
    public void Match_MultipleSources_ReportsIndependentMatches()
    {
        var sources = new[]
        {
            (SourceId: 1, RightAscension: 180.0, Declination: 0.0),
            (SourceId: 2, RightAscension: 200.0, Declination: 10.0),
        };

        var candidates = new[]
        {
            CatalogueMatchCandidate.Create("cat", "a", 180.0, 0.0),
            CatalogueMatchCandidate.Create("cat", "b", 200.0, 10.0),
        };

        var result = CatalogueCrossMatcher.Match(sources, candidates, radiusArcsec: 1.0);

        Assert.Equal(2, result.Value.Count);

        Assert.Contains(result.Value, m => m.SourceId == 1 && m.Candidate.Identifier == "a");

        Assert.Contains(result.Value, m => m.SourceId == 2 && m.Candidate.Identifier == "b");
    }

    [Fact]
    public void Match_NonPositiveRadius_ReturnsValidationError()
    {
        var result = CatalogueCrossMatcher.Match([], [], radiusArcsec: 0.0);

        Assert.True(result.IsFailure);

        Assert.Equal("catalogues.crossmatch.invalid_radius", result.Error.Code);
    }

    [Fact]
    public void Match_NoCandidates_ReturnsEmptyMatchList()
    {
        var sources = new[] { (SourceId: 1, RightAscension: 180.0, Declination: 0.0) };

        var result = CatalogueCrossMatcher.Match(sources, [], radiusArcsec: 5.0);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }
}
