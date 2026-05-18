using System.Net.Http.Json;
using QaPlatform.ApiClient.Models;
using QaPlatform.Core.Configuration;
using QaPlatform.Core.Stability;

namespace QaPlatform.ApiClient;

/// <summary>
/// Typed client for the Network Elements service.
///
/// <para>
/// One client class per <em>service domain</em>, not per endpoint
/// (see /architecture/system-architecture.md#api-client-layer).
/// </para>
/// <para>
/// Tests never construct an <see cref="HttpClient"/> directly — they get
/// a client from a fixture, which keeps connection pooling, base URL,
/// and auth concerns out of test bodies.
/// </para>
/// </summary>
public sealed class NetworkElementsClient
{
    private readonly HttpClient _http;

    public NetworkElementsClient(HttpClient http, EnvironmentConfig config)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(config);
        _http = http;
        if (_http.BaseAddress is null)
        {
            _http.BaseAddress = config.GetUri("ApiBaseUrl");
        }
    }

    /// <summary>
    /// Submits an onboarding request. Returns the accepted envelope —
    /// callers poll <see cref="WaitForTerminalStatusAsync"/> for completion.
    /// </summary>
    public async Task<HttpResponseMessage> OnboardAsync(
        OnboardElementRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _http.PostAsJsonAsync("/network-elements", request, ct);
    }

    /// <summary>
    /// Fetches the current status of an element.
    /// </summary>
    public async Task<ElementStatus> GetStatusAsync(string elementId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);
        var status = await _http.GetFromJsonAsync<ElementStatus>(
            $"/network-elements/{Uri.EscapeDataString(elementId)}/status", ct);
        return status ?? throw new InvalidOperationException(
            $"Server returned no body for element '{elementId}'.");
    }

    /// <summary>
    /// Polls the status endpoint until the element reaches a terminal status
    /// (<c>Ready</c> or <c>Failed</c>) or the timeout elapses.
    ///
    /// <para>
    /// Idempotent by design — safe to retry. Uses <see cref="Wait.For{T}"/>
    /// instead of an open-coded loop so the failure message includes the
    /// description and last observed value.
    /// </para>
    /// </summary>
    public ElementStatus WaitForTerminalStatus(
        string elementId,
        TimeSpan timeout)
    {
        return Wait.For(
            producer: () =>
            {
                var status = GetStatusAsync(elementId).GetAwaiter().GetResult();
                return IsTerminal(status.Status) ? status : null;
            },
            timeout: timeout,
            description: $"element '{elementId}' to reach a terminal status");
    }

    private static bool IsTerminal(string status) =>
        status.Equals("Ready", StringComparison.OrdinalIgnoreCase) ||
        status.Equals("Failed", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Deletes an element. Used primarily from cleanup hooks in the
    /// test data factory; returns true on 204, false on 404.
    /// </summary>
    public async Task<bool> DeleteAsync(string elementId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);
        var response = await _http.DeleteAsync(
            $"/network-elements/{Uri.EscapeDataString(elementId)}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        response.EnsureSuccessStatusCode();
        return true;
    }
}
