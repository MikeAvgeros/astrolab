namespace AstroLab.Core.Result;

public readonly record struct Error(string Code, string Message, ErrorCategory Category)
{
    public static Error Validation(string code, string message) => ErrorFactory.Create(code, message, ErrorCategory.Validation);

    public static Error NotFound(string code, string message) => ErrorFactory.Create(code, message, ErrorCategory.NotFound);

    public static Error Conflict(string code, string message) => ErrorFactory.Create(code, message, ErrorCategory.Conflict);

    public static Error Unauthorized(string code, string message) => ErrorFactory.Create(code, message, ErrorCategory.Unauthorized);

    public static Error Infrastructure(string code, string message) => ErrorFactory.Create(code, message, ErrorCategory.Infrastructure);

    public static Error NotImplemented(string code, string message) => ErrorFactory.Create(code, message, ErrorCategory.NotImplemented);

    public static Error Cancelled(string code, string message) => ErrorFactory.Create(code, message, ErrorCategory.Cancelled);

    public static Error Unexpected(string code, string message) => ErrorFactory.Create(code, message, ErrorCategory.Unexpected);

    public override string ToString() => $"[{Category}:{Code}] {Message}";
}

public static class ErrorFactory
{
    public static Error Create(string code, string message, ErrorCategory category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new Error(code, message, category);
    }
}
