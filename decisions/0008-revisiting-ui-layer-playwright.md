# ADR-0008: Revisiting the UI layer — Playwright if starting today

- **Status:** Proposed
- **Date:** 2026-Q2
- **Deciders:** QA platform lead (retrospective)
- **Supersedes:** [ADR-0002](0002-csharp-selenium-stack.md)

---

## Context

[ADR-0002](0002-csharp-selenium-stack.md) chose **C# + Selenium WebDriver** in 2020. That decision was defensible at the time and shipped a working platform.

Seven years on, three things have changed:

1. **The operator console is now a React SPA.** The 2020 console was server-rendered ASP.NET MVC with progressive enhancement. Selenium suited it. A modern SPA's testability surface — component-level data attributes, deterministic loading states, network mocking — fits Playwright's model much better than WebDriver's.
2. **Playwright has matured.** Auto-waiting, trace viewer, per-browser-context isolation, first-class network interception, and a debugging story that genuinely beats Selenium's. The 2020 reasons to reject Cypress (iframe support, multi-tab, team JavaScript skills) have all either been addressed by Playwright or have become non-issues as the team's TypeScript fluency grew.
3. **Frontend developers are the right people to write UI tests.** In 2020 the team was 25 backend / 3 frontend / 2 QA. In 2026 the frontend team is larger than the backend team. Locking UI tests to C# locked them out of the work.

This ADR proposes superseding [ADR-0002](0002-csharp-selenium-stack.md) **for the UI layer only**. Everything else from the 2020 stack (API tests in C#, JMeter for performance, the testing pyramid, the CI gate structure) stays.

## Decision

**If we were starting today, the UI layer would be Playwright + TypeScript, owned jointly by the frontend team and QA.**

Concretely, the proposed stack:

- **Playwright** (Node, current LTS) for browser automation
- **TypeScript** for test code, sharing the SPA's component types where useful
- **Same project structure as today:** `tests/QaPlatform.UiTests.Playwright/` lives next to `tests/QaPlatform.ApiTests/`, both run from the same `azure-pipelines.yml`
- **POM still applies.** The intent-vs-selectors discipline from [walkthroughs/01-pom-base-class](../code-samples/walkthroughs/01-pom-base-class/) is framework-agnostic and translates cleanly to Playwright's `Page` object pattern
- **Cleanup discipline still applies.** [ADR-0007](0007-cleanup-before-operation.md) is about test data, not the driver; the registry pattern works the same in TypeScript

What stays in C# / unchanged:
- API tests ([ADR-0001](0001-api-heavy-pyramid.md) pyramid intact)
- Test data factories
- The CI pipeline structure ([ADR-0005](0005-tests-as-blocking-gates.md))
- Vendor simulators ([ADR-0006](0006-vendor-simulators-over-hardware-lab.md))

This is a UI-layer swap, not a platform rewrite.

## Migration calculus (why this is `Proposed` not `Accepted`)

The honest cost picture:

| Cost | Estimate |
|---|---|
| Port existing UI test suite (~40 tests) | 4–6 weeks engineering |
| Dual-running both suites during transition | 6–8 weeks (CI cost + maintenance overhead) |
| Frontend team onboarding | 2–3 weeks elapsed |
| Tooling rewrite (CI scripts, docker-compose, reporting) | 2 weeks |

This is real work. The case for it is strong but not free. The status is `Proposed` because at the time of writing, the migration has not been scheduled. A future ADR will record either the migration happening (and supersede this one as `Accepted`) or the explicit decision to stay on Selenium (and supersede this one as `Rejected`).

## Consequences (if accepted)

**Good**
- UI test runtime drops materially — Playwright's auto-waiting and per-context parallelism are genuinely faster than Selenium WebDriver across multiple tabs.
- Trace viewer and `--ui` mode make debugging UI failures qualitatively easier. The current "watch the test run in headed mode three times to figure out what's flaky" loop largely goes away.
- Frontend developers can contribute. The biggest single change.
- Network interception becomes first-class. Today's pattern of "spin up a WireMock container next to the test" is still valid for API tests, but for UI tests, intercepting at the browser fetch layer is cleaner.

**Bad**
- A second language toolchain enters the test stack. Two CI configurations, two dependency managers, two sets of linter rules. The 2020 "one language for everything" win is partially undone.
- The shared-DTO benefit from [ADR-0002](0002-csharp-selenium-stack.md) is lost for UI tests. We'd need either a code-gen step (OpenAPI → TypeScript) or to accept some duplication.
- Existing UI test know-how (Selenium-specific patterns, the WebDriver mental model) becomes legacy. Engineers who built that expertise rightly ask what their next thing is.

**Neutral**
- The POM walkthrough and most of the framework-independent guidance in this repo stays valid. The patterns transfer; only the driver changes.

## Alternatives considered (2026)

- **Stay on Selenium.** Defensible if migration cost is genuinely prohibitive in a given quarter. Becomes harder to defend as the SPA evolves and Selenium's mismatch with modern frontend testing grows.
- **Cypress.** Considered. Playwright wins on multi-context support, multi-tab, trace viewer, and the steeper-but-shorter learning curve. Cypress's "all tests in the browser" model creates problems Playwright's "control the browser from outside" model does not.
- **WebDriverBidi (Selenium 5 with bidirectional protocol).** Genuinely promising. If a migration window doesn't open, this is the incremental upgrade path that gets some of Playwright's benefits without leaving the WebDriver world. Worth its own ADR if pursued.

## What this ADR is honest about

The 2020 Selenium decision was right *in 2020*. This ADR exists not to retroactively criticise it, but to make the platform's evolution visible. A portfolio that pretends every original decision was timelessly correct is a portfolio that learned nothing.

## Related

- [ADR-0002](0002-csharp-selenium-stack.md) — the decision this supersedes
- [ADR-0001](0001-api-heavy-pyramid.md) — the pyramid shape that limits how much UI surface this affects
- [code-samples/walkthroughs/01-pom-base-class/](../code-samples/walkthroughs/01-pom-base-class/) — the framework-independent patterns that survive the migration
