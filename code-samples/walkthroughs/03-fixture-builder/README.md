# 03 — Fixture Builder

> Why every builder registers its own cleanup, and why one failed teardown must never strand the rest.

**Code:** [src/QaPlatform.TestData/CleanupRegistry.cs](../../src/QaPlatform.TestData/CleanupRegistry.cs) · [src/QaPlatform.TestData/NetworkElementBuilder.cs](../../src/QaPlatform.TestData/NetworkElementBuilder.cs) · [src/QaPlatform.TestData/TestDataFactory.cs](../../src/QaPlatform.TestData/TestDataFactory.cs)

---

## The pattern

A test declares **what** it needs. The factory handles **how** to create and clean up.

```csharp
await using var factory = new TestDataFactory(client, nameof(MyTest));

var element = await factory.AnElement()
    .WithVendor("vendor-b")
    .WithRegion("us-east")
    .OnboardedAsync();

element.ShouldBeReady();
// Dispose runs cleanup automatically, in LIFO order, swallowing-but-logging failures.
```

No `try/finally`. No manual `DeleteAsync`. No "did I remember to clean that up?" review comments.

---

## Why this matters

### 1. Cleanup registered *before* the operation

This single decision in [NetworkElementBuilder](../../src/QaPlatform.TestData/NetworkElementBuilder.cs) prevents an entire class of bug:

```csharp
// Register cleanup BEFORE the operation so a partial failure
// still leaves something to tear down.
_cleanup.Register(
    $"delete element '{payload.ElementId}'",
    async () => await _client.DeleteAsync(payload.ElementId));

var response = await _client.OnboardAsync(payload);
```

If `OnboardAsync` half-creates the resource (202 accepted, then crash mid-provision), the cleanup is *already* registered. The naive "register on success" pattern would have leaked the resource.

### 2. LIFO order, like a destructor stack

The [CleanupRegistry](../../src/QaPlatform.TestData/CleanupRegistry.cs) is a stack, not a queue. Resources are torn down in reverse of creation, which is what most dependency graphs expect (delete the child element before the parent topology).

### 3. One failed cleanup must not strand the rest

Each cleanup runs in `try/catch`. A failure is **logged but not rethrown** — because a failed teardown should never mask a test result. The test you ran told you what you needed to know; the cleanup failure is operational noise that goes into the structured log.

```csharp
catch (Exception ex)
{
    _log.Warn("cleanup.failed", new {
        item.Description, error = ex.Message, type = ex.GetType().Name
    });
    // Intentionally swallow: see XML doc above.
}
```

Watch the JSON log lines from the actual test run:

```json
{"level":"INFO","test":"Factory_cleans_up_multiple_resources_in_reverse_order","message":"cleanup.ok","data":{"Description":"delete element 'el-ccc'"}}
{"level":"INFO","test":"Factory_cleans_up_multiple_resources_in_reverse_order","message":"cleanup.ok","data":{"Description":"delete element 'el-bbb'"}}
{"level":"INFO","test":"Factory_cleans_up_multiple_resources_in_reverse_order","message":"cleanup.ok","data":{"Description":"delete element 'el-aaa'"}}
```

LIFO. Auditable. Reverse of creation order.

### 4. `IAsyncDisposable` makes the test pattern impossible to forget

```csharp
await using var factory = new TestDataFactory(client, nameof(MyTest));
```

The `await using` makes cleanup automatic at scope exit. There is no "forgot to call dispose" failure mode — the language enforces it. This is the kind of pattern that shines in code review: any test that *doesn't* follow it stands out immediately.

---

## What this pattern is **not**

- **Not a generic "register any cleanup" utility.** It's tied to the test-data domain on purpose. Generic utilities accumulate every team's pet need and become unmaintainable.
- **Not a database transaction.** Real telecom resources can't be rolled back; they have to be explicitly deleted. The registry mirrors that reality.
- **Not a substitute for idempotent cleanup endpoints.** The `DeleteAsync` we call returns false on 404 — cleanup is best-effort, not a contract.

---

## See it pass

→ [tests/QaPlatform.ApiTests/TestDataFactoryTests.cs](../../tests/QaPlatform.ApiTests/TestDataFactoryTests.cs)

Two tests verify the pattern end-to-end: single-resource cleanup, and multi-resource LIFO ordering.

---

**Previous:** [02 — API Client Wrapper](../02-api-client-wrapper/README.md)
**Next:** [04 — Full E2E Test](../04-full-e2e-test/README.md)
