# ADR-0001: API-heavy testing pyramid

- **Status:** Accepted
- **Date:** 2020-Q2
- **Deciders:** QA platform lead, backend tech lead

---

## Context

The platform's UI was a thin operator console over a large REST/gRPC backend. Most business logic — onboarding workflows, multi-vendor reconciliation, policy enforcement — lived in services, not in the browser.

The team's instinct, coming from a manual-testing culture, was to automate what they already knew: clicking through the UI. The first proposed split was roughly **60% UI / 30% API / 10% unit**.

That would have killed us. UI tests against a multi-step onboarding flow took 45–90 seconds each. With a target of 250+ regression tests, a UI-dominant suite would have run for hours, flaked daily, and become the bottleneck it was meant to replace.

## Decision

Invert the pyramid toward APIs.

| Layer | Target share | Rationale |
|---|---|---|
| Unit | ~30% | Owned by dev teams; fast, deterministic |
| **API / integration** | **~50%** | **Where the business logic actually lives** |
| UI (E2E) | ~15% | A small set of "the whole stack still works" smoke journeys |
| Manual / exploratory | ~5% | Edge cases, new features in flight, accessibility |

UI tests cover **user journeys that cannot be expressed as API calls** (visual layout, multi-step wizards with browser state). Everything else is tested at the API boundary.

## Consequences

**Good**
- Regression suite went from a projected 4+ hours to ~25 minutes wall-clock with parallel sharding.
- Failures point to the actual broken layer. An API test failure is a backend bug; a UI test failure is genuinely a UI bug.
- API tests are stable across UI redesigns. The console was rewritten twice during the case study window; the API suite barely flinched.

**Bad**
- New QA engineers arrive expecting Selenium and have to be re-trained on HTTP, JSON, and contract thinking.
- A small set of cross-layer bugs (e.g., the API returns a field the UI doesn't render) are not caught by either API or UI tests alone. We mitigated with a handful of E2E smoke tests, but the gap is real.
- Stakeholders sometimes ask "but where are the UI tests?" because UI test counts are the legible metric. Educating them is ongoing work.

**Neutral**
- The split is a target, not a quota. Some quarters drifted to 55% API / 10% UI as new backend features landed faster than UI ones. We didn't force-balance it.

## Alternatives considered

- **UI-heavy (default instinct).** Rejected for reasons above: too slow, too flaky, tests the wrong layer.
- **No UI tests at all.** Rejected: the operator console is a customer-facing product. Shipping it untested would be negligent, even if the API beneath is well covered.
- **Contract tests (Pact-style) instead of API integration tests.** Considered seriously; deferred. The platform consumed too many vendor APIs whose providers wouldn't participate in a contract-testing handshake. Worth revisiting today.

## Related

- [architecture/testing-pyramid.md](../architecture/testing-pyramid.md) — the visual + numeric breakdown
- [code-samples/tests/QaPlatform.ApiTests/](../code-samples/tests/QaPlatform.ApiTests/) — the API-layer pattern in practice
- [ADR-0002](0002-csharp-selenium-stack.md) — the UI-layer choice that this pyramid de-emphasises
