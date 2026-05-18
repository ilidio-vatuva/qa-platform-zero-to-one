namespace QaPlatform.Core.Stability;

/// <summary>
/// Retry helper for <em>idempotent</em> operations only.
///
/// <para>
/// This is deliberately not a generic "retry any exception" utility. Retrying
/// non-idempotent operations is how a test suite ends up double-creating
/// resources, double-billing things, or — in the original telecom context —
/// double-onboarding network elements.
/// </para>
/// <para>
/// Callers must pass an explicit <see cref="RetryPolicy"/> describing which
/// exceptions are retryable, the maximum attempts, and the backoff. Anything
/// else propagates immediately.
/// </para>
/// </summary>
public static class IdempotentRetry
{
    public static T Execute<T>(Func<T> operation, RetryPolicy policy, string description)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Exception? lastException = null;
        for (var attempt = 1; attempt <= policy.MaxAttempts; attempt++)
        {
            try
            {
                return operation();
            }
            catch (Exception ex) when (policy.IsRetryable(ex))
            {
                lastException = ex;
                if (attempt == policy.MaxAttempts) break;
                Thread.Sleep(policy.DelayFor(attempt));
            }
        }

        throw new RetryExhaustedException(
            $"'{description}' failed after {policy.MaxAttempts} attempts.", lastException);
    }
}

public sealed class RetryPolicy
{
    public int MaxAttempts { get; }
    public TimeSpan BaseDelay { get; }
    public Func<Exception, bool> IsRetryable { get; }

    public RetryPolicy(int maxAttempts, TimeSpan baseDelay, Func<Exception, bool> isRetryable)
    {
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        MaxAttempts = maxAttempts;
        BaseDelay = baseDelay;
        IsRetryable = isRetryable ?? throw new ArgumentNullException(nameof(isRetryable));
    }

    /// <summary>Exponential backoff with a hard ceiling at 30s.</summary>
    public TimeSpan DelayFor(int attempt)
    {
        var ms = BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        return TimeSpan.FromMilliseconds(Math.Min(ms, 30_000));
    }
}

public sealed class RetryExhaustedException : Exception
{
    public RetryExhaustedException(string message, Exception? inner) : base(message, inner) { }
}
