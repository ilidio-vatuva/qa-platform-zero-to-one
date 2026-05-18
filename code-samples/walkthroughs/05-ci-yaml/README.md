# 05 — CI YAML

> Why the QA pipeline *is* the quality contract, not the implementation of one.

**Code:** [infra/azure-pipelines.yml](../../infra/azure-pipelines.yml) · [infra/Dockerfile.test-runner](../../infra/Dockerfile.test-runner) · [infra/docker-compose.test.yml](../../infra/docker-compose.test.yml)

---

## Which pipeline is this?

This YAML models the **QA pipeline** — the post-deployment pipeline owned by the QA platform team. It is **not** the developer CI/CD.

In the real platform:

- The **dev CI/CD** (separate Azure DevOps project, owned by dev teams) ran build + unit + lint on every PR. Fast. Blocked merge.
- The **QA pipeline** (this YAML) was triggered after Octopus deployed a main-branch build to staging. It ran API and UI suites against the deployed build. Blocked **release-candidate promotion**, not PR merge.

This split is the load-bearing decision recorded in [ADR-0005](../../../decisions/0005-tests-as-blocking-gates.md) and diagrammed in [test-execution-flow.md](../../../architecture/test-execution-flow.md). Read those if you want the *why*; this walkthrough is the *how*.

---

## The pipeline at a glance

```
Build            warnings-as-errors → publish workspace artefact
    ↓            (in the real platform, build lived in the dev CI; reproduced
                  here for self-containment)
ApiTests         4 parallel shards, Category!=UI
    ↓
UiTests          matrix: chrome + firefox, each backed by a service container
    ↓
ReleaseCandidate main-branch only, deploys to "staging" environment (= approval gate)
```

Each stage is a hard gate. Anything red blocks the next stage. Override is *possible* — via the Azure DevOps `staging` environment's required-approvers — and recorded, and reviewed in retro. Friction is the point.

---

## Why these specific choices

### 1. `dotnet build … /warnaserror` in the Build stage

> "These samples are meant to model the discipline they describe."

A warning that's allowed to live is a warning that breeds others. Treating them as errors at the **earliest** stage is cheaper than discovering on Friday afternoon that 200 of them have accumulated.

### 2. Sharding via `strategy: parallel: 4`

The shape mirrors [test-execution-flow.md](../../../architecture/test-execution-flow.md#2-pull-request-build-58-minutes):

> *"The shard count was tuned so that **any single shard finished in under 3 minutes** — that became the team's wall-clock budget for parallelisable work."*

The YAML doesn't pretend to *be* the sharding logic — it just runs the test runner with `$(System.JobPositionInPhase)`. The actual filter (which test goes to which shard) lives in the runner. **Pipelines should orchestrate; logic belongs in code.**

### 3. UI tests as a `matrix` over browsers, with `services:`

```yaml
strategy:
  matrix:
    chrome:  { browser: chrome }
    firefox: { browser: firefox }
services:
  selenium: selenium/standalone-$(browser):4.21
```

Each browser job gets its own Selenium sidecar container — no shared grid, no cross-job contention. This is the same per-shard isolation principle from [environments.md](../../../architecture/environments.md#2-isolated-per-shard), expressed in CI.

### 4. The "ReleaseCandidate" stage is a *deployment*, not a script

```yaml
- deployment: rc
  environment: staging
```

The `environment: staging` line is what gates the stage on Azure DevOps environment approvals. The approvals themselves aren't in YAML — they're configured in the AzDO project. **The YAML declares the gate; the project enforces it.** This separation is the only sustainable way to keep emergency overrides auditable.

### 5. Test results published with `condition: succeededOrFailed()`

```yaml
- task: PublishTestResults@2
  condition: succeededOrFailed()
```

A red build that *doesn't* publish its artefacts is a debugging black hole. The default is `succeeded()`, which would skip the publish step on failure — the worst possible time to skip it. This single override has paid for itself many times over.

---

## What's deliberately not in this YAML

- **No flaky-test auto-retry.** Retries on flakes were a defeat; the flake-quarantine workflow in [test-execution-flow.md](../../../architecture/test-execution-flow.md#how-failures-were-handled) lives outside the pipeline and is reviewed weekly.
- **No coverage gates.** A coverage % is a lagging proxy for the wrong thing. The team watched flake rate and time-to-green, not coverage trend.
- **No performance suite.** Perf runs nightly, against a separate SRE-owned dashboard. See [walkthrough placement note in performance/README.md](../../performance/README.md).
- **No "deploy to prod"** stage. Prod is an operator-facing canary handled outside this pipeline.

---

## What the Docker side does for this story

The [Dockerfile.test-runner](../../infra/Dockerfile.test-runner) and [docker-compose.test.yml](../../infra/docker-compose.test.yml) make the same runner work locally as in CI:

- **Same image.** The runner CI uses is built from the same Dockerfile a developer can `docker build` locally.
- **Same entrypoint.** [run-shard.sh](../../infra/run-shard.sh) is what both Compose and CI invoke.
- **Same env contract.** `QA_ENVIRONMENT`, `SHARD_INDEX`, `SHARD_TOTAL` are read the same way everywhere.

This is the discipline that prevents "works on CI, fails locally" — and its inverse, which is worse.

---

## The pitch in one sentence

The QA pipeline isn't a script that runs tests; it's the **enforcement layer** that makes the architecture documented in [/architecture](../../../architecture/) real. The dev CI/CD is the contract for code health; this pipeline is the contract for release readiness. Without both, every other discipline in this repo is voluntary.

---

**Previous:** [04 — Full E2E Test](../04-full-e2e-test/README.md)

That's the end of the walkthrough series. From here, go read the real code in [/code-samples/src](../../src) or the architecture in [/architecture](../../../architecture/).
