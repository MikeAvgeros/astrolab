namespace AstroLab.Core.Result;

/// <summary>Non-generic factory helpers that improve type inference at call sites.</summary>
public static class Result
{
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);
}
