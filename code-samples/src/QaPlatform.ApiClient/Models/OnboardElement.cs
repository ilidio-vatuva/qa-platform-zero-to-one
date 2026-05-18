namespace QaPlatform.ApiClient.Models;

/// <summary>
/// Request payload for onboarding a network element.
/// Mirrors a generic version of a real telecom onboarding contract.
/// </summary>
public sealed record OnboardElementRequest(
    string ElementId,
    string Vendor,
    string Region,
    IReadOnlyDictionary<string, string> Tags);

/// <summary>
/// Response returned when the platform accepts an onboarding request.
/// Onboarding is asynchronous — clients poll <see cref="StatusUrl"/> until
/// the element reaches a terminal state.
/// </summary>
public sealed record OnboardElementResponse(
    string ElementId,
    string Status,
    Uri StatusUrl);

public sealed record ElementStatus(
    string ElementId,
    string Status,
    string? FailureReason);
