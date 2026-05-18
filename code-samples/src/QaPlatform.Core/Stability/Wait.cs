namespace QaPlatform.Core.Stability;

/// <summary>
/// Explicit, bounded, conditional waits.
///
/// <para>
/// Why this exists: <c>Thread.Sleep</c> was a PR-block offence on this platform
/// (see architecture/system-architecture.md UI Layer). Tests poll for a condition
/// to become true within a deadline, and fail loudly with the last observed value
/// when it doesn't — never silently swallow the timeout.
/// </para>
/// </summary>
public static class Wait
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Polls <paramref name="condition"/> until it returns true or <paramref name="timeout"/> elapses.
    /// Throws <see cref="TimeoutException"/> with diagnostic context on failure.
    /// </summary>
    public static void Until(
        Func<bool> condition,
        TimeSpan timeout,
        string description,
        TimeSpan? pollInterval = null)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var interval = pollInterval ?? DefaultPollInterval;
        var deadline = DateTimeOffset.UtcNow + timeout;
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                if (condition())
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            Thread.Sleep(interval);
        }

        throw new TimeoutException(
            $"Timed out after {timeout.TotalSeconds:F1}s waiting for: {description}",
            lastException);
    }

    /// <summary>
    /// Polls <paramref name="producer"/> until it returns a non-default value, and returns it.
    /// Useful for "wait for the API to give me the resource I just created".
    /// </summary>
    public static T For<T>(
        Func<T?> producer,
        TimeSpan timeout,
        string description,
        TimeSpan? pollInterval = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(producer);

        T? value = null;
        Until(() =>
        {
            value = producer();
            return value is not null;
        }, timeout, description, pollInterval);

        return value!;
    }
}
