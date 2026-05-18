# ADR-0007: Register cleanup *before* the operation (LIFO)

- **Status:** Accepted
- **Date:** 2020-Q3
- **Deciders:** QA platform lead

---

## Context

Test cleanup is where most test suites quietly rot. The standard patterns are all subtly wrong:

- **`try/finally` per test.** Works for one resource. Falls apart the moment a test creates three resources that depend on each other, because the `finally` runs in source order and your dependent resource fails to delete because its parent is already gone.
- **`[TestCleanup]` / `IDisposable`.** Same problem, plus the cleanup code is far from the creation code, so it drifts as the test changes.
- **"We'll clean it up in a nightly job."** This is what people say right before their staging environment fills up with 40,000 orphaned test resources.

There was also a more insidious failure mode: tests that create resource A, then create resource B, then *the second creation fails*. With naive cleanup, A is leaked because the test never reached its cleanup block.

## Decision

**Cleanup is registered with a per-test `CleanupRegistry` *before* the operation that creates the resource. Cleanup runs LIFO (last-in, first-out) regardless of how the test exits.**

The pattern looks like:

```csharp
await using var factory = new TestDataFactory(client, logger);

// Cleanup is registered FIRST. If the create call below throws,
// cleanup still runs and the partial state gets reaped.
var element = await factory.OnboardNetworkElement(builder => builder
    .WithVendor("ericsson")
    .WithRegion("eu-west"));

// Test body...
// On dispose, cleanups run in reverse order of registration.
```

Internally, `TestDataFactory.OnboardNetworkElement` calls `_registry.Register(() => client.Delete(id))` **before** it calls `client.Create(...)`. If `Create` throws, the registered cleanup either:

- Successfully deletes the partially-created resource (if it exists), or
- Gets a 404 from the delete call, which it swallows and logs.

Either outcome is safe.

LIFO matters because dependent resources are usually created in dependency order (parent → child) and must be deleted in the reverse order (child → parent). LIFO is the only ordering that gets this right by default.

See [CleanupRegistry.cs](../code-samples/src/QaPlatform.TestData/CleanupRegistry.cs) and the walkthrough in [walkthroughs/03-fixture-builder](../code-samples/walkthroughs/03-fixture-builder/).

## Consequences

**Good**
- Zero leaked resources across the case-study window once the pattern was universal. (Before it: ~40 orphans/week in staging.)
- Tests that throw in the middle of a setup chain still clean up everything they did register. The "partial setup" failure mode is closed.
- The `await using var factory = ...` line is the same across every test. New engineers learn the pattern once.
- The registry logs each cleanup as a structured event (ADR-0004), so leak investigations are grep-able.

**Bad**
- Registering cleanup *before* the operation feels backwards. New engineers consistently write it the wrong way around in their first few PRs and have to be coached.
- A cleanup that fails for a non-404 reason (auth expired, network blip) gets swallowed-but-logged. We chose that deliberately — failing the test on a cleanup error would mask the actual test outcome — but it does mean leaks can recur silently if nobody watches the cleanup-failed log channel.
- The `CleanupRegistry` is per-test-class scope. Cross-test resource sharing (which we discourage anyway) doesn't work with it.

**Neutral**
- The pattern is enforced by code review, not by the type system. A determined engineer can still call `client.Create` without registering cleanup. We accept this; the alternative (a fluent API that physically cannot be misused) is more clever than the problem warrants.

## Alternatives considered

- **`try/finally` per resource.** Rejected: doesn't compose across multiple resources, doesn't survive setup-time failures.
- **Test-class-level `[OneTimeTearDown]`.** Rejected: same composition problem, plus the cleanup code drifts far from the creation code.
- **A pool of pre-created resources that tests check out and return.** Considered for read-heavy fixtures. Rejected for the platform's onboarding-heavy scenarios because the resources are typed by vendor/region/configuration and a pool would need to be huge to cover the matrix.
- **"Cleanup by tag" — every test tags its resources, a sweeper deletes them later.** Used as a safety-net backstop, not as the primary mechanism. The registry catches 99.9%; the sweeper catches the leaks the registry couldn't (e.g., test process was hard-killed).

## Related

- [code-samples/src/QaPlatform.TestData/CleanupRegistry.cs](../code-samples/src/QaPlatform.TestData/CleanupRegistry.cs) — the implementation
- [code-samples/walkthroughs/03-fixture-builder/](../code-samples/walkthroughs/03-fixture-builder/) — the walkthrough with JSON log excerpts
- [ADR-0004](0004-structured-json-logging.md) — the logging that makes leak investigations tractable
