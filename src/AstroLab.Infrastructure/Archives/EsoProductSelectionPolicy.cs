namespace AstroLab.Infrastructure.Archives;

public static class EsoProductSelectionPolicy
{
    private const string PublicDataRights = "PUBLIC";
    private const string PrimaryProductSemantics = "#this";
    private const string FitsExtension = ".fits";
    private const string FitsFormatToken = "fits";
    private const int PublicDataRightsScore = 1000;
    private const int PrimaryProductScore = 100;
    private const int FitsFormatScore = 500;
    private const int CalibrationLevelScoreMultiplier = 10;

    public static EsoProduct? SelectBest(IReadOnlyList<EsoProduct> products)
    {
        EsoProduct? best = null;
        var bestScore = int.MinValue;

        foreach (var product in products)
        {
            var score = Score(product);

            if (score <= bestScore) continue;
            
            bestScore = score;
            
            best = product;
        }

        return best;
    }

    private static int Score(EsoProduct product)
    {
        var score = 0;

        if (string.Equals(product.DataRights, PublicDataRights, StringComparison.OrdinalIgnoreCase))
        {
            score += PublicDataRightsScore;
        }

        if (string.Equals(product.ProductType, PrimaryProductSemantics, StringComparison.OrdinalIgnoreCase))
        {
            score += PrimaryProductScore;
        }

        if (LooksLikeFits(product))
        {
            score += FitsFormatScore;
        }

        score += (product.CalibrationLevel ?? 0) * CalibrationLevelScoreMultiplier;

        return score;
    }

    private static bool LooksLikeFits(EsoProduct product)
    {
        if (product.Format?.Contains(FitsFormatToken, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        var name = product.FileName ?? product.DataUri;

        return name.EndsWith(FitsExtension, StringComparison.OrdinalIgnoreCase);
    }
}
