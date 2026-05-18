using QaPlatform.ApiClient;
using QaPlatform.ApiClient.Assertions;
using QaPlatform.ApiClient.Builders;
using QaPlatform.ApiClient.Models;

namespace QaPlatform.TestData;

/// <summary>
/// Builds a network element via the API and registers its cleanup.
///
/// <para>
/// Usage:
/// <code>
/// var element = await factory.AnElement()
///     .WithVendor("vendor-b")
///     .WithRegion("us-east")
///     .OnboardedAsync();
/// </code>
/// </para>
/// <para>
/// The builder does two things real product builders did on the original
/// platform: it sets sensible defaults so tests only declare what matters,
/// and it registers a delete hook with the <see cref="CleanupRegistry"/>
/// so the test never has to remember teardown.
/// </para>
/// </summary>
public sealed class NetworkElementBuilder
{
    private readonly NetworkElementsClient _client;
    private readonly CleanupRegistry _cleanup;
    private readonly OnboardElementRequestBuilder _request = new();
    private TimeSpan _readyTimeout = TimeSpan.FromSeconds(30);

    internal NetworkElementBuilder(NetworkElementsClient client, CleanupRegistry cleanup)
    {
        _client = client;
        _cleanup = cleanup;
    }

    public NetworkElementBuilder WithElementId(string id)
    {
        _request.WithElementId(id);
        return this;
    }

    public NetworkElementBuilder WithVendor(string vendor)
    {
        _request.WithVendor(vendor);
        return this;
    }

    public NetworkElementBuilder WithRegion(string region)
    {
        _request.WithRegion(region);
        return this;
    }

    public NetworkElementBuilder WithTag(string key, string value)
    {
        _request.WithTag(key, value);
        return this;
    }

    public NetworkElementBuilder ReadyWithin(TimeSpan timeout)
    {
        _readyTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Submits the onboarding request, waits for terminal status, asserts
    /// <c>Ready</c>, and registers cleanup. Returns the final status.
    /// </summary>
    public async Task<ElementStatus> OnboardedAsync()
    {
        var payload = _request.Build();

        // Register cleanup BEFORE the operation so a partial failure
        // still leaves something to tear down.
        _cleanup.Register(
            $"delete element '{payload.ElementId}'",
            async () => await _client.DeleteAsync(payload.ElementId));

        var response = await _client.OnboardAsync(payload);
        var envelope = response.ShouldBeAcceptedWithStatusUrl();

        var terminal = _client.WaitForTerminalStatus(envelope.ElementId, _readyTimeout);
        terminal.ShouldBeReady();
        return terminal;
    }
}
