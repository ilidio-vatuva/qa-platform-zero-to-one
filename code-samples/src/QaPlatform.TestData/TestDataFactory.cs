using QaPlatform.ApiClient;

namespace QaPlatform.TestData;

/// <summary>
/// Composition root for test data builders.
///
/// <para>
/// One factory per test. It owns the <see cref="CleanupRegistry"/> and hands
/// builders the dependencies they need. Test bodies stay free of plumbing:
/// </para>
/// <code>
/// await using var factory = new TestDataFactory(client, nameof(MyTest));
/// var el = await factory.AnElement().WithRegion("eu-west").OnboardedAsync();
/// // ... assertions ...
/// // disposal runs cleanup automatically
/// </code>
/// </summary>
public sealed class TestDataFactory : IAsyncDisposable
{
    private readonly NetworkElementsClient _client;
    private readonly CleanupRegistry _cleanup;

    public TestDataFactory(NetworkElementsClient client, string testName)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _cleanup = new CleanupRegistry(testName);
    }

    /// <summary>Begin building a network element. Cleanup is registered on build.</summary>
    public NetworkElementBuilder AnElement() => new(_client, _cleanup);

    public ValueTask DisposeAsync() => _cleanup.DisposeAsync();
}
