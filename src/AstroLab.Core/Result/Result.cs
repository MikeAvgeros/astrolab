namespace AstroLab.Core.Result;

public readonly record struct Result<TValue>
{
    private readonly TValue? _value;
    private readonly Error _error;

    private Result(bool isSuccess, TValue? value, Error error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;
    
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result. Check IsSuccess or use Match.");
    
    public Error Error => IsFailure
        ? _error
        : throw new InvalidOperationException("Cannot access Error on a successful Result. Check IsFailure or use Match.");

    public static Result<TValue> Success(TValue value) => new(true, value, default);

    public static Result<TValue> Failure(Error error) => new(false, default, error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);
    
    public void Deconstruct(out bool isSuccess, out TValue? value, out Error error)
    {
        isSuccess = IsSuccess;
        value = _value;
        error = _error;
    }
    
    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error);
    
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

    public Result<TOut> Map<TOut>(Func<TValue, TOut> map) =>
        IsSuccess ? Result<TOut>.Success(map(_value!)) : Result<TOut>.Failure(_error);
    
    public Result<TOut> Bind<TOut>(Func<TValue, Result<TOut>> bind) =>
        IsSuccess ? bind(_value!) : Result<TOut>.Failure(_error);
    
    public Result<TValue> MapError(Func<Error, Error> map) =>
        IsSuccess ? this : Failure(map(_error));
    
    public Result<TValue> Ensure(Func<TValue, bool> predicate, Error error) =>
        IsSuccess && !predicate(_value!) ? Failure(error) : this;
    
    public TValue GetValueOrDefault(TValue fallback) => IsSuccess ? _value! : fallback;

    public override string ToString() => IsSuccess ? $"Success({_value})" : $"Failure({_error})";
}
