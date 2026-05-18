# ADR-0005: Tests as blocking quality gates

- **Status:** Accepted
- **Date:** 2020-Q3
- **Deciders:** QA platform lead, engineering manager, head of platform

---

## Context

A test suite that doesn't block *something* is a suite that decays. The default failure mode is well-documented across the industry: tests start green, drift to "this one's been red for a week, ignore it", and within six months 30% of the suite is permanently red and nobody knows which failures are real.

In Phase 1 the suite was advisory. Failures generated a Slack notification. Developers were "encouraged" to look at them. The flake rate climbed steadily and the trust collapsed within two months.

Phase 2 needed a different model. The constraint was: API and UI tests **could not** live on the per-PR developer CI/CD. They were too slow, required a *deployed* build (not a unit-test-style in-process fake), and were owned by a different team. Putting them on every PR would have pushed PR feedback past ~20 minutes and broken the dev-CI bargain (see [test-execution-flow.md](../architecture/test-execution-flow.md)).

The question was therefore not "blocking or not" — it was **which pipeline gates which thing**.

## Decision

**Two pipelines, two gate tiers.**

- The **dev CI/CD** (per-PR, owned by dev teams) blocks PR merge on build, static analysis, and unit tests.
- The **QA pipeline** (post-deployment, owned by the QA platform team, in a separate Azure DevOps project) blocks **release-candidate promotion** on API/integration and UI suites against a deployed build on staging.
- Performance runs nightly on a different cadence again; it informs, it does not block.

| Pipeline | Stage | What it blocks |
|---|---|---|
| Dev CI/CD | Build (warnings-as-errors) | **Blocks PR merge.** A new warning is a regression. |
| Dev CI/CD | Unit tests | **Blocks PR merge.** |
| Dev CI/CD | Lint / format | **Blocks PR merge.** |
| QA pipeline | API tests (sharded against deployed build) | **Blocks RC promotion.** Does not block PR merge. |
| QA pipeline | UI tests (Chrome + Firefox matrix) | **Blocks RC promotion.** Does not block PR merge. |
| QA pipeline | Staging RC suite | **Blocks customer deployment.** Requires explicit human approval. |
| Performance (scheduled) | JMeter baseline | Informational. SLO violations open a ticket. |

The sample [azure-pipelines.yml](../code-samples/infra/azure-pipelines.yml) in this repo models the **QA pipeline** specifically — the dev CI/CD existed in a different Azure DevOps project and isn't reproduced here.

The two-pipeline split is the load-bearing design choice. It admits the honest reality: API and UI tests are expensive enough that you cannot afford to put them in front of every developer commit, but they are also too important to leave un-gated. The compromise is to gate them at the next-most-expensive step — promotion to release candidate — and to make that gate uncompromising.

## Consequences

**Good**
- The suite stayed honest. Flake rate held under 2% across the case-study window because flaky tests *had to be fixed* — there was no "we'll get to it" option, once a build was sitting un-promoted because of them.
- The dev CI/CD stayed fast (~4 min median). Developers never had the political ammunition to argue for weaker gates, because the gates they actually waited on were build + unit and those were already near-optimal.
- Developers started asking "how do I make this testable?" within about three sprints of the policy taking effect — even though the QA pipeline ran post-merge, the cultural shift happened anyway, because a red QA build *with their PR's name on it* was visible to the whole team.
- The release-cycle metric (2–3 weeks → 3–4 days) is mostly attributable to this split. Blocking gates compress the feedback loop from "human review" to "automated within the hour"; non-blocking-on-PR gates keep that hour from becoming five.

**Bad**
- A regression introduced in a PR does not surface until **after** merge, when the QA pipeline runs. Median 25–35 minutes from merge to first QA verdict. We accepted this; the alternative was worse. But it does mean main can go red for reasons no individual PR-author noticed.
- The standing rule when main went red was **revert first, debug second**. This works but feels harsh to authors whose PR was the unlucky one to expose a flake.
- A small number of senior developers pushed back hard on "the QA team blocking my release". The response was to make the test failures and quarantine policy fully transparent — anyone could see why a test failed, anyone could quarantine a confirmed flake — but the political tax was real, and the two-pipeline split made the QA team's gate more visible (it had its own dashboard, its own Azure DevOps project) than if it had been buried inside the dev CI/CD.
- One incident: a critical hotfix was delayed by 90 minutes because an unrelated UI test was flaky in the QA pipeline. We added a documented "emergency bypass" procedure (requires two approvers, must be paid back within 48h) after that. It's been used three times.

**Neutral**
- The quarantine flow (see [test-execution-flow.md](../architecture/test-execution-flow.md)) is part of the cost. A test that fails twice in 24 hours gets auto-quarantined and assigned a ticket. Without that, the policy would be unsustainable.
- The two-pipeline split required cross-team ownership clarity. Dev teams owned the dev CI/CD config; QA owned the QA pipeline. The handoff (Octopus deployment to staging) was the seam. When the seam broke, it took both teams to fix it.

## Alternatives considered

- **Advisory only (Phase 1 model).** Tried. Failed. Documented above.
- **Put API and UI on the dev CI/CD, blocking PR merge.** The "ideal" textbook setup. Rejected because: (a) it would have pushed PR feedback past 20 minutes once the suite scaled, (b) API tests need a *deployed* build, not an in-process one — the deploy-and-test loop on every PR was operationally infeasible at the time, (c) it would have given dev teams a hard incentive to weaken or skip the QA suite, defeating the point.
- **Run the QA suite per-PR but non-blocking.** Considered. Rejected because non-blocking gates *are* advisory gates and we already knew how that story ends (see Context).
- **Block merges only on tests the developer wrote in that PR.** Clever, but unworkable — most regressions are by definition not in the PR that introduced them. The whole point of regression testing is to catch unintended impact.
- **One unified pipeline owned by a "platform" team that does both dev CI and QA.** Considered. Would have required a team that didn't exist and a political agreement about who owns what. The two-pipeline split was the org-chart-shaped solution that was actually shippable.

## Related

- [architecture/test-execution-flow.md](../architecture/test-execution-flow.md) — the two-pipeline sequence diagram and the gate ownership table
- [architecture/system-architecture.md](../architecture/system-architecture.md) — the CI / CD section with both pipelines drawn out
- [code-samples/infra/azure-pipelines.yml](../code-samples/infra/azure-pipelines.yml) — the QA pipeline modelled in YAML (dev CI/CD is not reproduced here; it lived in a separate Azure DevOps project)
- [code-samples/walkthroughs/05-ci-yaml/](../code-samples/walkthroughs/05-ci-yaml/) — the QA pipeline as an enforcement layer
- [ADR-0001](0001-api-heavy-pyramid.md) — why the QA suite is API-heavy (which is what makes its ~7 min wall-clock against a deployed build possible at all)
