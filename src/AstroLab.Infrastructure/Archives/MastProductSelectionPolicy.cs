namespace AstroLab.Infrastructure.Archives;

public static class MastProductSelectionPolicy
{
    private const string FitsExtension = ".fits";
    private const string PublicDataRights = "PUBLIC";
    private const string ScienceProductType = "SCIENCE";
    private const int PublicDataRightsScore = 1000;
    private const int ScienceProductTypeScore = 100;
    private const int CalibrationLevelScoreMultiplier = 10;

    public static MastProduct? SelectBest(IReadOnlyList<MastProduct> products)
    {
        MastProduct? best = null;
        var bestScore = int.MinValue;

        foreach (var product in products)
        {
            if (!IsFitsProduct(product))
            {
                continue;
            }

            var score = Score(product);

            if (score > bestScore)
            {
                bestScore = score;
                best = product;
            }
        }

        return best;
    }

    private static bool IsFitsProduct(MastProduct product)
    {
        var name = product.Filename ?? product.DataUri;

        return name.EndsWith(FitsExtension, StringComparison.OrdinalIgnoreCase);
    }

    private static int Score(MastProduct product)
    {
        var score = 0;

        if (string.Equals(product.DataRights, PublicDataRights, StringComparison.OrdinalIgnoreCase))
        {
            score += PublicDataRightsScore;
        }

        if (string.Equals(product.ProductType, ScienceProductType, StringComparison.OrdinalIgnoreCase))
        {
            score += ScienceProductTypeScore;
        }

        score += (product.CalibrationLevel ?? 0) * CalibrationLevelScoreMultiplier;

        return score;
    }
}
