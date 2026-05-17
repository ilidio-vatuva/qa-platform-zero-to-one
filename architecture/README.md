# Architecture

> How the QA platform was structured — and **why** those structural choices held up under Tier 1 telecom pressure.

This section is written for the engineering manager who will read the [project README](../README.md), nod, and then ask: *"OK, but show me how it actually fits together."*

---

## Reading order

1. **[Testing Pyramid](./testing-pyramid.md)** — the layered strategy and what *doesn't* get automated, and why.
2. **[System Architecture](./system-architecture.md)** — the components of the framework and how they interact.
3. **[Test Execution Flow](./test-execution-flow.md)** — what happens between a developer's commit and a quality-gated release.
4. **[Environment Strategy](./environments.md)** — Docker, parallelization, and the multi-vendor reality of telecom.

Each document is self-contained. If you only have 5 minutes, read the pyramid.

---

## Architectural principles

Four principles shaped every decision documented in this folder. They are not aspirational — they were the tie-breakers when trade-offs got hard.

### 1. Stability over novelty
A flaky test is worse than no test. Every tool choice (C#, Selenium + POM, JMeter, Docker) was made because the team could reason about its failure modes, not because it was on a conference slide. Novel tools were evaluated, but the bar was: *does this remove a real bottleneck, or just add resume value?*

### 2. Tests are production code
The test suite was treated with the same discipline as the product: code review, versioning, refactors, deprecation cycles. No "throwaway scripts." If a test wasn't worth maintaining, it wasn't worth writing.

### 3. The pipeline is the contract
Quality gates lived in CI, not in a person's judgment. The release manager couldn't override a red build with a meeting. This forced the team to invest in test reliability, because the pipeline's authority depended on it.

### 4. Design for the bad day
Telecom doesn't tolerate "we'll fix it forward." The architecture assumed: tests *will* fail intermittently, environments *will* drift, a vendor API *will* change without notice. Every component had to fail loudly, diagnose itself, and be cheap to re-run.

---

## What this architecture is not

- **Not a reference implementation.** Other domains (consumer web, mobile, fintech) will have different constraints. The pyramid here is heavier at the API/integration layer than a typical web app would justify.
- **Not the final state.** This is the architecture as it stabilised around Phase 2. The Phase 3 evolution (mentorship, culture, AI-augmented QA exploration) is documented in [lessons-learned.md](../lessons-learned.md) and [decisions/](../decisions/).
- **Not vendor-specific.** Names of orchestrators, network elements, and customer environments are generalised. The patterns transfer; the proprietary glue does not.

---

## A note on diagrams

All diagrams in this folder are written in [Mermaid](https://mermaid.js.org/) so they render natively on GitHub and stay version-controlled alongside the prose. If a diagram and the text disagree, the text wins — diagrams are a navigation aid, not the source of truth.
