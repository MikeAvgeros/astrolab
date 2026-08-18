namespace AstroLab.Core.Result;

/// <summary>
/// A lightweight, immutable description of a domain failure. <see cref="Error"/> is a
/// value type so that failure paths through <see cref="Result{TValue}"/> never allocate
/// on the managed heap beyond the message string itself.
/// </summary>
/// <param name="Code">A short, stable, machine-readable identifier (e.g. "fits.header.missing_naxis").</param>
/// <param name="Message">A human-readable description of the failure.</param>
/// <param name="Category">The broad classification used to map the error onto a transport response.</param>
public readonly record struct Error(string Code, string Message, ErrorCategory Category)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorCategory.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorCategory.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorCategory.Conflict);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorCategory.Unauthorized);

    public static Error Infrastructure(string code, string message) => new(code, message, ErrorCategory.Infrastructure);

    public static Error NotImplemented(string code, string message) => new(code, message, ErrorCategory.NotImplemented);

    public static Error Cancelled(string code, string message) => new(code, message, ErrorCategory.Cancelled);

    public static Error Unexpected(string code, string message) => new(code, message, ErrorCategory.Unexpected);

    public override string ToString() => $"[{Category}:{Code}] {Message}";
}
