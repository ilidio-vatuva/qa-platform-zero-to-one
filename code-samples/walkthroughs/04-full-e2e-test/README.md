# 04 — Full E2E Test

> The moment the three previous patterns compose into a single readable test.

**Code:** [tests/QaPlatform.ApiTests/TestDataFactoryTests.cs](../../tests/QaPlatform.ApiTests/TestDataFactoryTests.cs)

---

## What "the patterns composing" actually looks like

Here is a real, passing test from the suite:

```csharp
[Fact]
public async Task Factory_creates_a_ready_element_and_cleans_up_on_dispose()
{
    _fake.GivenOnboardingImmediatelyReadyFor("el-factory-001");

    await using var factory = new TestDataFactory(
        _client,
        nameof(Factory_creates_a_ready_element_and_cleans_up_on_dispose));

    var element = await factory.AnElement()
        .WithElementId("el-factory-001")
        .WithVendor("vendor-b")
        .WithRegion("us-east")
        .WithTag("env", "test")
        .OnboardedAsync();

    element.ShouldBeReady();
}
```

That body fits on one screen. There is no `HttpClient` construction, no JSON building, no polling loop, no `try/finally`, no `await Task.Delay(...)`. **Every concern from the previous walkthroughs has been pushed into a layer.**

---

## What each line proves about the architecture

| Line | Pattern from | Architecture claim it demonstrates |
|---|---|---|
| `_fake.GivenOnboardingImmediatelyReadyFor("el-factory-001")` | step 2 | Tests target a fake when running locally; same client code works against staging ([system-architecture.md §Seams](../../../architecture/system-architecture.md#1-test-code--product-api)) |
| `await using var factory = new TestDataFactory(...)` | step 3 | Cleanup is impossible to forget — `await using` enforces it |
| `factory.AnElement().WithVendor(...).WithRegion(...).OnboardedAsync()` | step 3 + step 2 | Builder declares intent; cleanup registers before the operation; client owns the HTTP |
| `element.ShouldBeReady()` | step 2 | Co-located assertion, domain-specific failure message |

There are no surprises in the test body because all the discipline is in the layers it stands on.

---

## What this would look like *without* the patterns

The same flow, written naively against `HttpClient`:

```csharp
[Fact]
public async Task Onboard_an_element_naive()
{
    using var http = new HttpClient { BaseAddress = new Uri(_fake.BaseUrl) };
    var json = JsonSerializer.Serialize(new {
        elementId = "el-factory-001", vendor = "vendor-b",
        region = "us-east", tags = new Dictionary<string, string> { ["env"] = "test" }
    });

    var resp = await http.PostAsync("/network-elements",
        new StringContent(json, Encoding.UTF8, "application/json"));
    Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);

    var envelope = JsonSerializer.Deserialize<OnboardElementResponse>(
        await resp.Content.ReadAsStringAsync());

    // Poll for status...
    var deadline = DateTime.UtcNow.AddSeconds(5);
    string? status = null;
    while (DateTime.UtcNow < deadline)
    {
        var s = await http.GetFromJsonAsync<ElementStatus>(
            $"/network-elements/{envelope!.ElementId}/status");
        if (s?.Status is "Ready" or "Failed") { status = s.Status; break; }
        await Task.Delay(200);
    }
    Assert.Equal("Ready", status);

    // Don't forget to clean up!
    try
    {
        await http.DeleteAsync($"/network-elements/el-factory-001");
    }
    catch { /* hope for the best */ }
}
```

Three screens. Five different failure modes that could land on the same line number. A `Task.Delay` polling loop that doesn't tell you what went wrong when it times out. A cleanup block whose `catch` swallows everything, including the actual signal you needed.

Now imagine 200 of those.

---

## The point

The walkthrough order — POM → API client → fixture builder → composed test — is the order the patterns *had to be built* on the original platform. Each one made the next one cheaper. Skip any of them and the suite ossifies long before it gets to 250+ tests.

The five-minute pitch to a hiring manager is: **"the readable test on this page exists because three other things in this repo make it possible."**

---

**Previous:** [03 — Fixture Builder](../03-fixture-builder/README.md)
**Next:** [05 — CI YAML](../05-ci-yaml/README.md)
