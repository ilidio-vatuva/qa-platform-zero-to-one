# 02 — API Client Wrapper

> Why every HTTP call goes through a typed C# wrapper, and why assertions live next to the client that produces them.

**Code:** [src/QaPlatform.ApiClient/NetworkElementsClient.cs](../../src/QaPlatform.ApiClient/NetworkElementsClient.cs) · [src/QaPlatform.ApiClient/Builders/OnboardElementRequestBuilder.cs](../../src/QaPlatform.ApiClient/Builders/OnboardElementRequestBuilder.cs) · [src/QaPlatform.ApiClient/Assertions/NetworkElementsAssertions.cs](../../src/QaPlatform.ApiClient/Assertions/NetworkElementsAssertions.cs)

---

## The pattern

Three pieces compose into a single readable line of test code:

```csharp
var response = await _client.OnboardAsync(
    new OnboardElementRequestBuilder()
        .WithVendor("vendor-b")
        .WithRegion("us-east")
        .Build());

var envelope = response.ShouldBeAcceptedWithStatusUrl();
```

- The **client** owns the HTTP surface — one class per service *domain* (not per endpoint).
- The **builder** owns sensible defaults; tests override only what matters.
- The **assertion** extension lives next to the client; reads as English, fails with a useful message.

---

## Why this matters

### 1. Contract drift becomes a compile error, not a 2 a.m. alert

When the product changes an endpoint's shape, exactly one file in the test code changes: the typed client. Hundreds of tests adapt automatically because they never knew the URL or the wire format.

Without this firewall, every endpoint change ripples across the suite. We've seen test sprawl projects die from this; ours didn't.

### 2. One client per domain — not per endpoint

A class called `OnboardElementClient` is over-decomposition: it creates a vacuum where related operations (delete, status, list) have no obvious home. So they get scattered. So they get duplicated.

[NetworkElementsClient](../../src/QaPlatform.ApiClient/NetworkElementsClient.cs) owns the whole domain. Adding a new endpoint is adding a method, not adding a class.

### 3. Builders make tests document their intent

```csharp
new OnboardElementRequestBuilder()
    .WithVendor("vendor-b")
    .WithRegion("us-east")
    .Build();
```

vs. the constructor alternative:

```csharp
new OnboardElementRequest("el-xxx", "vendor-b", "us-east",
    new Dictionary<string, string>());
```

The builder shows what the test cares about. The constructor shows what the type happens to require. The first is documentation; the second is noise.

### 4. Co-located assertions are not a DSL — they're vocabulary

[NetworkElementsAssertions](../../src/QaPlatform.ApiClient/Assertions/NetworkElementsAssertions.cs) lives next to the client it asserts on. Extensions read as English:

```csharp
response.ShouldBeAcceptedWithStatusUrl();
status.ShouldBeReady();
```

The failure messages include the actual status code, the response body, and the element ID — so a red CI link doesn't require a debugger to triage.

This is **not** FluentAssertions or Shouldly — those are general-purpose. Our assertions are *domain-specific*, which is exactly what makes them more readable than a generic `Should().Be(202)`.

---

## The polling story

Onboarding is asynchronous. The client provides:

```csharp
public ElementStatus WaitForTerminalStatus(string elementId, TimeSpan timeout)
```

…which delegates to [`Wait.For`](../../src/QaPlatform.Core/Stability/Wait.cs) from the Core layer. Same diagnostic contract everywhere: failure messages include the description and the last observed value. No silent timeouts.

---

## See it pass

The test:

→ [tests/QaPlatform.ApiTests/NetworkElementOnboardingTests.cs](../../tests/QaPlatform.ApiTests/NetworkElementOnboardingTests.cs)

It runs against an in-process [WireMock.Net](https://github.com/WireMock-Net/WireMock.Net) fake — no Docker, no external services. `dotnet test` works on a clean clone.

---

**Previous:** [01 — POM Base Class](../01-pom-base-class/README.md)
**Next:** [03 — Fixture Builder](../03-fixture-builder/README.md)
