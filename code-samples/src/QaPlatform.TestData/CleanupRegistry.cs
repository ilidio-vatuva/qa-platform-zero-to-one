using QaPlatform.Core.Logging;

namespace QaPlatform.TestData;

/// <summary>
/// LIFO registry of cleanup actions for a single test.
///
/// <para>
/// Each builder registers a cleanup hook when it creates a resource. At
/// teardown the registry runs them in reverse order — so resources are
/// torn down in the inverse of their creation, which is what most
/// dependency graphs expect.
/// </para>
/// <para>
/// Every cleanup is wrapped in a <c>try/catch</c>: one failure must not
/// strand the rest. Failures are logged but do not throw, because a
/// failed teardown should never mask a test result.
/// </para>
/// </summary>
public sealed class CleanupRegistry : IAsyncDisposable
{
    private readonly Stack<CleanupAction> _actions = new();
    private readonly TestLogger _log;
    private readonly string _testName;

    public CleanupRegistry(string testName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testName);
        _testName = testName;
        _log = TestLogger.For(testName);
    }

    /// <summary>
    /// Register an async cleanup action. The <paramref name="description"/>
    /// is included in log output so a failed teardown is identifiable.
    /// </summary>
    public void Register(string description, Func<Task> action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(action);
        _actions.Push(new CleanupAction(description, action));
    }

    public async ValueTask DisposeAsync()
    {
        while (_actions.Count > 0)
        {
            var item = _actions.Pop();
            try
            {
                await item.Action();
                _log.Info("cleanup.ok", new { item.Description });
            }
            catch (Exception ex)
            {
                _log.Warn("cleanup.failed", new
                {
                    item.Description,
                    error = ex.Message,
                    type = ex.GetType().Name
                });
                // Intentionally swallow: see XML doc above.
            }
        }
    }

    private sealed record CleanupAction(string Description, Func<Task> Action);
}
