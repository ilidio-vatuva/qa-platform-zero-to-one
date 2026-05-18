# ADR-0002: C# + Selenium for UI automation

- **Status:** **Superseded by [ADR-0008](0008-revisiting-ui-layer-playwright.md)** (2026-Q2)
- **Date:** 2020-Q2
- **Deciders:** QA platform lead, backend tech lead, engineering manager

---

## Context

The platform's backend was C# / .NET. The operator console was a server-rendered ASP.NET MVC app with progressive enhancement (jQuery, some early React islands). The engineering team was ~25 backend developers, ~3 frontend, and a brand-new QA team of 2.

**Selenium WebDriver and C# were already in the building.** A handful of exploratory Selenium scripts existed from an earlier proof-of-concept, the dev toolchain (Visual Studio, NuGet, the Azure DevOps build agents) was already provisioned for .NET, and every engineer who would ever review a test PR already read C# fluently.

The real question was not *"which stack do we pick from a clean slate?"*. It was *"do we commit to what's already here, or do we pay a switching cost to chase something better?"*.

A hypothetical switch — to Java + Selenium, or to JavaScript + Cypress (then v3, just maturing) — would have meant:

- A second language toolchain in the build (new SDK, new package manager, new CI agent image).
- Retraining or rehiring. The backend team would not contribute to a JavaScript test suite; the QA team of 2 had no JavaScript experience.
- Throwing away the exploratory Selenium scripts and the institutional knowledge around them.
- Months of "we're not shipping tests because we're still setting up the framework" — at exactly the moment Phase 1's credibility depended on shipping tests.

No candidate alternative offered a benefit that justified that cost in 2020.

## Decision

**Commit to C# + Selenium WebDriver, formalised with the Page Object Model.**

This is not a discovery decision — it's a *commit* decision. We are choosing to invest in what's already here rather than reset.

Test projects live in the same Git repo as the product code, use the same `.csproj` tooling, the same NuGet feed, the same CI pipeline. A backend developer can open the solution and add a test the same day.

Page Object Model was non-negotiable: it forces the "intent vs selectors" split that survives UI refactors. See [walkthroughs/01-pom-base-class](../code-samples/walkthroughs/01-pom-base-class/).

## Consequences

**Good**
- Developer contributions to the test suite were real — about 20% of API tests and 5% of UI tests came from non-QA engineers during the case-study window. That's the metric that mattered.
- Shared types between production code and tests (request/response DTOs) eliminated a whole class of "the test assumes the API field is called `name` but it's actually `displayName`" bugs.
- Selenium's WebDriver protocol is boring and well-documented. When tests broke, the failure mode was almost always understandable.

**Bad**
- Selenium tests are slow. The cross-browser nature (multiple HTTP round-trips per action) puts a floor on how fast a UI test can run, no matter how clean the code.
- Flakiness from implicit waits, stale elements, and timing races was a permanent tax. We mitigated with `Wait.Until` patterns (see [Stability/Wait.cs](../code-samples/src/QaPlatform.Core/Stability/Wait.cs)), but never eliminated.
- The C# choice locked out frontend developers who lived in TypeScript. They never contributed a UI test, even though they were the people most qualified to write them.

**Neutral**
- The Page Object Model adds a layer of indirection. New engineers ask "why can't I just call `driver.FindElement` directly?" — and the answer takes a 20-minute explanation. Worth it, but a tax.

## Alternatives considered

- **Greenfield Java + Selenium.** Same WebDriver story, but a second toolchain for zero benefit — the backend was C# and the build agents were already .NET. Rejected.
- **Greenfield Cypress (2020 version).** Considered seriously despite the switching cost. Rejected at the time because: (a) iframe-heavy parts of the console were unsupported, (b) no multi-tab support, (c) the team had zero JavaScript experience, (d) the switching cost (toolchain, CI, retraining, lost momentum) was real and not recoverable inside the Phase 1 window. All of (a)–(c) have since been addressed by Cypress and especially Playwright. See [ADR-0008](0008-revisiting-ui-layer-playwright.md).
- **Protractor.** Tied to Angular. Console wasn't Angular. Trivially rejected.
- **Ruby + Watir.** Legacy from a prior contractor. Nobody on the team knew Ruby. Rejected.
- **"Do nothing, keep the exploratory scripts as-is."** The implicit alternative. Rejected because the existing scripts had no POM, no waits discipline, no CI integration — they would not scale past 20 tests.

## Why this was superseded

By 2024–2026 the trade-off shifted:
- The console rewrite to a React SPA broke a lot of Selenium-era assumptions.
- Playwright matured: trace viewer, auto-waiting, true parallel isolation per browser context.
- Frontend developers became the people who *should* be writing UI tests, and they live in TypeScript.

See [ADR-0008](0008-revisiting-ui-layer-playwright.md) for the proposed replacement and the migration calculus.

## Related

- [architecture/system-architecture.md](../architecture/system-architecture.md) — the layers Selenium drives
- [code-samples/src/QaPlatform.Ui/](../code-samples/src/QaPlatform.Ui/) — the POM in practice
- [ADR-0001](0001-api-heavy-pyramid.md) — why the UI layer is intentionally small to begin with
- [ADR-0008](0008-revisiting-ui-layer-playwright.md) — the replacement
