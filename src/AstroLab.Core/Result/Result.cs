namespace AstroLab.Core.Result;

/// <summary>
/// A discriminated union representing the outcome of an operation that either
/// succeeds with a <typeparamref name="TValue"/> or fails with an <see cref="Result.Error"/>.
/// </summary>
/// <remarks>
/// <para>
/// C# does not yet ship a native discriminated-union type, so this struct provides the
/// canonical two-case union by hand: exactly one of a success value or a failure
/// <see cref="Error"/> is ever populated, and the state can only be observed through
/// <see cref="Match{TResult}"/>, <see cref="Deconstruct"/>, or the <see cref="IsSuccess"/> /
/// <see cref="IsFailure"/> discriminant — which is what allows the compiler's pattern-matching
/// exhaustiveness checks (property patterns, switch expressions) to reason about it as if it
/// were a true union.
/// </para>
/// <para>Prefer this type over throwing exceptions for any outcome that is part of normal,
/// expected control flow (validation failures, missing data, calculation failures).</para>
/// </remarks>
/// <typeparam name="TValue">The type produced by a successful operation.</typeparam>
public readonly struct Result<TValue>
{
    private readonly TValue? _value;
    private readonly Error _error;

    private Result(bool isSuccess, TValue? value, Error error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    /// <summary>Discriminant of the union. <see langword="true"/> when a value is present.</summary>
    public bool IsSuccess { get; }

    /// <summary>Discriminant of the union. <see langword="true"/> when an <see cref="Error"/> is present.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The success value. Throws <see cref="InvalidOperationException"/> if the result is a
    /// failure — this is a programmer-error guard, not a domain control-flow exception; callers
    /// should branch on <see cref="IsSuccess"/> or use <see cref="Match{TResult}"/> instead.
    /// </summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result. Check IsSuccess or use Match.");

    /// <summary>
    /// The failure error. Throws <see cref="InvalidOperationException"/> if the result succeeded.
    /// </summary>
    public Error Error => IsFailure
        ? _error
        : throw new InvalidOperationException("Cannot access Error on a successful Result. Check IsFailure or use Match.");

    public static Result<TValue> Success(TValue value) => new(true, value, default);

    public static Result<TValue> Failure(Error error) => new(false, default, error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);

    /// <summary>Deconstructs the union for positional pattern matching, e.g. <c>result is (true, var value, _)</c>.</summary>
    public void Deconstruct(out bool isSuccess, out TValue? value, out Error error)
    {
        isSuccess = IsSuccess;
        value = _value;
        error = _error;
    }

    /// <summary>Reduces the union to a single value by applying the matching branch.</summary>
    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error);

    /// <summary>Executes the matching side effect for the current branch of the union.</summary>
    public void Switch(Action<TValue> onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess)
        {
            onSuccess(_value!);
        }
        else
        {
            onFailure(_error);
        }
    }

    /// <summary>Transforms the success value, passing failures through unchanged.</summary>
    public Result<TOut> Map<TOut>(Func<TValue, TOut> map) =>
        IsSuccess ? Result<TOut>.Success(map(_value!)) : Result<TOut>.Failure(_error);

    /// <summary>Chains a follow-up operation that itself returns a <see cref="Result{TValue}"/> (monadic bind).</summary>
    public Result<TOut> Bind<TOut>(Func<TValue, Result<TOut>> bind) =>
        IsSuccess ? bind(_value!) : Result<TOut>.Failure(_error);

    /// <summary>Transforms a failure's error, passing successes through unchanged.</summary>
    public Result<TValue> MapError(Func<Error, Error> map) =>
        IsSuccess ? this : Failure(map(_error));

    /// <summary>Downgrades a success to a failure when <paramref name="predicate"/> does not hold.</summary>
    public Result<TValue> Ensure(Func<TValue, bool> predicate, Error error) =>
        IsSuccess && !predicate(_value!) ? Failure(error) : this;

    /// <summary>Returns the success value, or <paramref name="fallback"/> when the result failed.</summary>
    public TValue GetValueOrDefault(TValue fallback) => IsSuccess ? _value! : fallback;

    public override string ToString() => IsSuccess ? $"Success({_value})" : $"Failure({_error})";
}
