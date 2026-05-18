using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace QaPlatform.ApiTests.Fakes;

/// <summary>
/// In-process fake of the Network Elements service.
///
/// <para>
/// Why this exists: we want <c>dotnet test</c> to work on a clean clone with
/// no Docker, no external services, no fixtures. WireMock.Net stands up an
/// HTTP server on a free port, returns canned responses, and tears down with
/// the fixture.
/// </para>
/// <para>
/// In CI against staging this fake is replaced by the real service —
/// the <see cref="QaPlatform.ApiClient.NetworkElementsClient"/> code is unchanged.
/// </para>
/// </summary>
public sealed class FakeNetworkElementsService : IDisposable
{
    private readonly WireMockServer _server;

    public FakeNetworkElementsService()
    {
        _server = WireMockServer.Start();
    }

    public Uri BaseUrl => new(_server.Url!);

    /// <summary>
    /// Configures the fake so that onboarding the given element succeeds:
    /// 202 on POST, then a status that progresses Pending -> Ready on the
    /// second status poll. Mirrors the real service's async behaviour.
    /// </summary>
    public void GivenOnboardingSucceedsFor(string elementId)
    {
        _server
            .Given(Request.Create()
                .WithPath("/network-elements")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(202)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""
                    {
                      "elementId": "{{elementId}}",
                      "status": "Pending",
                      "statusUrl": "{{_server.Url}}/network-elements/{{elementId}}/status"
                    }
                    """));

        // First poll: still Pending. Second poll onwards: Ready.
        var statusPath = $"/network-elements/{elementId}/status";
        _server
            .Given(Request.Create().WithPath(statusPath).UsingGet())
            .InScenario("onboarding")
            .WillSetStateTo("polled-once")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""{"elementId":"{{elementId}}","status":"Pending","failureReason":null}"""));

        _server
            .Given(Request.Create().WithPath(statusPath).UsingGet())
            .InScenario("onboarding")
            .WhenStateIs("polled-once")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""{"elementId":"{{elementId}}","status":"Ready","failureReason":null}"""));
    }

    /// <summary>Configures cleanup deletes to succeed with 204.</summary>
    public void GivenDeleteSucceeds()
    {
        _server
            .Given(Request.Create()
                .WithPath(new WireMock.Matchers.RegexMatcher("^/network-elements/[^/]+$"))
                .UsingDelete())
            .RespondWith(Response.Create().WithStatusCode(204));
    }

    /// <summary>
    /// Configures the fake so that the given element id is onboarded successfully
    /// and reaches <c>Ready</c> on the first status poll (no scenario state).
    /// Use this when a test pins an id explicitly and doesn't need to observe
    /// the intermediate Pending state.
    /// </summary>
    public void GivenOnboardingImmediatelyReadyFor(string elementId)
    {
        _server
            .Given(Request.Create()
                .WithPath("/network-elements")
                .UsingPost()
                .WithBody(new WireMock.Matchers.JsonPartialMatcher(new { elementId })))
            .RespondWith(Response.Create()
                .WithStatusCode(202)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""
                    {
                      "elementId": "{{elementId}}",
                      "status": "Pending",
                      "statusUrl": "{{_server.Url}}/network-elements/{{elementId}}/status"
                    }
                    """));

        _server
            .Given(Request.Create()
                .WithPath($"/network-elements/{elementId}/status")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""{"elementId":"{{elementId}}","status":"Ready","failureReason":null}"""));
    }

    public void Dispose() => _server.Dispose();
}
