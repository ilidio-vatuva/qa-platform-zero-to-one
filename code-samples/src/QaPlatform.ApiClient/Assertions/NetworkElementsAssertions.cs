using System.Net;
using System.Net.Http.Json;
using QaPlatform.ApiClient.Models;

namespace QaPlatform.ApiClient.Assertions;

/// <summary>
/// Assertion extensions co-located with the API client they apply to.
///
/// <para>
/// Why co-located: a reader of an integration test should be able to
/// understand the assertion without leaving the file or learning a DSL.
/// <c>response.ShouldBeAcceptedWithLocation()</c> reads as English and
/// fails with a message that points at the actual status code and body.
/// </para>
/// </summary>
public static class NetworkElementsAssertions
{
    /// <summary>
    /// Asserts the response is <c>202 Accepted</c> with a deserialisable
    /// <see cref="OnboardElementResponse"/> body whose <c>StatusUrl</c> is set.
    /// Returns the parsed envelope for further inspection.
    /// </summary>
    public static OnboardElementResponse ShouldBeAcceptedWithStatusUrl(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new AssertionException(
                $"Expected 202 Accepted but got {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }

        var envelope = response.Content
            .ReadFromJsonAsync<OnboardElementResponse>()
            .GetAwaiter().GetResult();

        if (envelope is null)
        {
            throw new AssertionException("Response body was empty; expected an OnboardElementResponse.");
        }

        if (envelope.StatusUrl is null)
        {
            throw new AssertionException(
                $"OnboardElementResponse.StatusUrl was null for element '{envelope.ElementId}'.");
        }

        return envelope;
    }

    /// <summary>Asserts the element reached the <c>Ready</c> terminal status.</summary>
    public static void ShouldBeReady(this ElementStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (!"Ready".Equals(status.Status, StringComparison.OrdinalIgnoreCase))
        {
            throw new AssertionException(
                $"Expected element '{status.ElementId}' to be Ready but was '{status.Status}'. " +
                $"Failure reason: {status.FailureReason ?? "<none>"}");
        }
    }
}

public sealed class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
