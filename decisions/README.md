# Decisions

> **Framing.** This folder belongs to a [composite case study](../README.md) — the trade-offs and consequences are real, identifying details (companies, vendor names, exact dates, specific numbers) are generalised. ADR-0008 is the only entry explicitly marked as a hypothetical-future revisit. Everything else describes what was actually decided.

Architecture Decision Records (ADRs) for the QA platform. Each ADR captures **one trade-off** — the context that forced a choice, the choice itself, and what we accepted by making it.

> ADRs are **immutable** once accepted. If a decision changes, a new ADR supersedes the old one. The old one stays in the repo with a `Superseded` status so the evolution is visible.

---

## Why ADRs exist in this repo

The architecture docs explain **what** the platform looks like. The walkthroughs explain **how** the code is shaped. ADRs explain **why** — specifically, why the obvious alternative was rejected.

If you read an architecture doc and think *"this could just as easily have been done another way"*, the answer is in here.

---

## Index

| # | Title | Status | Date |
|---|---|---|---|
| [0000](0000-adr-template.md) | ADR template & process | Accepted | 2020-Q2 |
| [0001](0001-api-heavy-pyramid.md) | API-heavy testing pyramid | Accepted | 2020-Q2 |
| [0002](0002-csharp-selenium-stack.md) | C# + Selenium for UI automation | **Superseded** by [0008](0008-revisiting-ui-layer-playwright.md) | 2020-Q2 |
| [0003](0003-no-pre-prod-environment.md) | Staging carries release-candidate duty (no pre-prod) | Accepted | 2020-Q3 |
| [0004](0004-structured-json-logging.md) | Structured JSON logging from day one | Accepted | 2020-Q2 |
| [0005](0005-tests-as-blocking-gates.md) | Tests as blocking quality gates | Accepted | 2020-Q3 |
| [0006](0006-vendor-simulators-over-hardware-lab.md) | Vendor simulators over a shared hardware lab | Accepted | 2020-Q3 |
| [0007](0007-cleanup-before-operation.md) | Register cleanup **before** the operation (LIFO) | Accepted | 2020-Q3 |
| [0008](0008-revisiting-ui-layer-playwright.md) | Revisiting the UI layer: Playwright if starting today | Proposed | 2026-Q2 |

---

## Reading order

- **Hiring manager / staff engineer** — Read [0001](0001-api-heavy-pyramid.md), [0003](0003-no-pre-prod-environment.md), [0005](0005-tests-as-blocking-gates.md). These are the decisions with the biggest business impact.
- **SDET / QA engineer** — Read [0002](0002-csharp-selenium-stack.md) then [0008](0008-revisiting-ui-layer-playwright.md) back-to-back (the evolution arc), plus [0004](0004-structured-json-logging.md) and [0007](0007-cleanup-before-operation.md) for the patterns you'll meet in the code.
- **Platform / SRE** — [0003](0003-no-pre-prod-environment.md) and [0006](0006-vendor-simulators-over-hardware-lab.md).

---

## What's deliberately NOT here

- **Tooling versions** — those live in the code samples and walkthroughs; an ADR shouldn't break when xUnit ships a new minor.
- **Implementation details** — ADRs answer "why this approach", not "how to wire it up". The how lives in [/walkthroughs](../code-samples/walkthroughs/).
- **Retrospective regrets that didn't actually change anything** — those belong in [lessons-learned.md](../lessons-learned.md). ADRs are decisions we either still stand by or have formally superseded.
