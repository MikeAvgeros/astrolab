using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class SpectralTypeClassifierTests
{
    [Fact]
    public void Classify_ReturnsNotImplemented()
    {
        ReadOnlySpan<double> spectrum = [1.0, 2.0, 3.0];

        var result = SpectralTypeClassifier.Classify(spectrum);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.classification.not_implemented", result.Error.Code);

        Assert.Equal(AstroLab.Core.Result.ErrorCategory.NotImplemented, result.Error.Category);
    }

    [Fact]
    public void MethodName_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(SpectralTypeClassifier.MethodName));
    }
}
