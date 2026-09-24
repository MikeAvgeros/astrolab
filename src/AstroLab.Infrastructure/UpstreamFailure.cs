using AstroLab.Core.Result;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace AstroLab.Infrastructure;

/// <summary>
/// Classifies exceptions raised while calling an external archive or catalogue service. Network
/// failures, per-attempt or overall timeouts, exhausted retries and an open circuit breaker all mean
/// the upstream service was unavailable, which is an expected infrastructure failure (HTTP 502) rather
/// than an unexpected server error. A cancellation that the caller did not request is an HttpClient
/// or resilience-pipeline timeout, so it is classified the same way.
/// </summary>
internal static class UpstreamFailure
{
    public static bool IsUnavailable(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        HttpRequestException or TimeoutRejectedException or BrokenCircuitException => true,
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false,
    };

    public static Error ToError(string errorCodePrefix, string serviceName) => Error.Infrastructure(
        $"{errorCodePrefix}_unavailable",
        $"{serviceName} did not respond successfully (network failure, timeout, or repeated upstream errors). Try again later.");
}
