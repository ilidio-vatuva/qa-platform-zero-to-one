using QaPlatform.ApiClient;
using QaPlatform.ApiClient.Assertions;
using QaPlatform.ApiClient.Builders;
using QaPlatform.ApiTests.Fakes;
using QaPlatform.Core.Configuration;

namespace QaPlatform.ApiTests;

/// <summary>
/// Demonstrates the full API-layer pattern end-to-end:
///   1. Build the request with a builder (readable defaults, override only what matters)
///   2. Submit via the typed client
///   3. Assert the envelope with co-located assertion extensions
///   4. Wait for a terminal status using bounded explicit polling
///   5. Clean up
///
/// The test runs against an in-process WireMock fake — no network, no Docker.
/// </summary>
public sealed class NetworkElementOnboardingTests : IDisposable
{
    private readonly FakeNetworkElementsService _fake;
    private readonly HttpClient _http;
    private readonly NetworkElementsClient _client;

    public NetworkElementOnboardingTests()
    {
        _fake = new FakeNetworkElementsService();

        // Set the base URL directly on HttpClient so tests don't fight over
        // process-wide env vars when xUnit runs them in parallel.
        _http = new HttpClient { BaseAddress = _fake.BaseUrl };
        var config = new EnvironmentConfig(TestEnvironment.Local);
        _client = new NetworkElementsClient(_http, config);
    }

    [Fact]
    public async Task Onboarding_a_new_element_reaches_Ready()
    {
        // Arrange
        var request = new OnboardElementRequestBuilder()
            .WithElementId("el-test-001")
            .WithVendor("vendor-a")
            .WithRegion("eu-west")
            .WithTag("env", "test")
            .Build();

        _fake.GivenOnboardingSucceedsFor(request.ElementId);
        _fake.GivenDeleteSucceeds();

        try
        {
            // Act
            var response = await _client.OnboardAsync(request);
            var envelope = response.ShouldBeAcceptedWithStatusUrl();

            var terminal = _client.WaitForTerminalStatus(
                envelope.ElementId,
                timeout: TimeSpan.FromSeconds(5));

            // Assert
            terminal.ShouldBeReady();
        }
        finally
        {
            // Cleanup-on-failure: this runs even if asserts above threw.
            await _client.DeleteAsync(request.ElementId);
        }
    }

    public void Dispose()
    {
        _http.Dispose();
        _fake.Dispose();
    }
}
