using AstroLab.Core.Result;

namespace AstroLab.Tests.Core;

public class ResultTests
{
    [Fact]
    public void Success_ExposesValue_AndIsSuccessTrue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);

        Assert.False(result.IsFailure);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_ExposesError_AndIsFailureTrue()
    {
        var error = Error.Validation("test.code", "something went wrong");

        var result = Result<int>.Failure(error);

        Assert.False(result.IsSuccess);

        Assert.True(result.IsFailure);

        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Value_OnFailure_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Failure(Error.Unexpected("x", "y"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Error_OnSuccess_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Success(1);

        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ProducesSuccess()
    {
        Result<int> result = 7;

        Assert.True(result.IsSuccess);

        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_ProducesFailure()
    {
        Result<int> result = Error.NotFound("x", "not found");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Match_InvokesCorrectBranch_ForSuccessAndFailure()
    {
        Result<int> success = 5;

        Result<int> failure = Error.Unexpected("x", "y");

        Assert.Equal("ok:5", success.Match(v => $"ok:{v}", e => $"err:{e.Code}"));

        Assert.Equal("err:x", failure.Match(v => $"ok:{v}", e => $"err:{e.Code}"));
    }

    [Fact]
    public void PatternMatching_WithSwitchExpression_DiscriminatesOnIsSuccess()
    {
        Result<int> success = 5;

        Result<int> failure = Error.Unexpected("boom", "y");

        static string Describe(Result<int> r) => r switch
        {
            { IsSuccess: true } s => $"value={s.Value}",
            { IsSuccess: false } f => $"error={f.Error.Code}",
        };

        Assert.Equal("value=5", Describe(success));

        Assert.Equal("error=boom", Describe(failure));
    }

    [Fact]
    public void Deconstruct_ExposesDiscriminantValueAndError()
    {
        Result<int> success = 9;

        var (isSuccess, value, error) = success;

        Assert.True(isSuccess);

        Assert.Equal(9, value);

        Assert.Equal(default, error);
    }

    [Fact]
    public void Map_TransformsSuccessValue_LeavesFailureUnchanged()
    {
        Result<int> success = 3;

        Result<int> failure = Error.Unexpected("x", "y");

        Assert.Equal(6, success.Map(v => v * 2).Value);

        Assert.True(failure.Map(v => v * 2).IsFailure);
    }

    [Fact]
    public void Bind_ChainsFollowUpResult_ShortCircuitsOnFailure()
    {
        static Result<int> Halve(int v) => v % 2 == 0
            ? Result<int>.Success(v / 2)
            : Error.Validation("odd", "value must be even");

        Assert.Equal(5, Result<int>.Success(10).Bind(Halve).Value);

        Assert.True(Result<int>.Success(7).Bind(Halve).IsFailure);

        Assert.True(Result<int>.Failure(Error.Unexpected("x", "y")).Bind(Halve).IsFailure);
    }

    [Fact]
    public void Ensure_DowngradesSuccessToFailure_WhenPredicateFails()
    {
        var error = Error.Validation("negative", "must be positive");

        var result = Result<int>.Success(-1).Ensure(v => v > 0, error);

        Assert.True(result.IsFailure);

        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void GetValueOrDefault_ReturnsFallback_OnFailure()
    {
        Result<int> failure = Error.Unexpected("x", "y");

        Assert.Equal(99, failure.GetValueOrDefault(99));
    }
}
