# Code Samples

> Illustrative-but-runnable C# implementations of the patterns described in [/architecture](../architecture/). Every file here compiles, every project builds, and every test runs on a clean clone.

The goal is not a full product. The goal is that a peer SDET can `git clone`, `dotnet test`, and see the patterns from the architecture docs working in real code.

> **Start here:** the [walkthroughs/](walkthroughs/) folder is the narrative tour of this sample suite — five numbered docs, ~30 minutes total, each linking into the real code.

---

## Status

This section is being built incrementally. What's currently in the repo:

| Layer | Project | Status |
|---|---|---|
| Core abstractions | [src/QaPlatform.Core](src/QaPlatform.Core) | ✅ available |
| API client + assertions | [src/QaPlatform.ApiClient](src/QaPlatform.ApiClient) | ✅ available |
| Test data factory | [src/QaPlatform.TestData](src/QaPlatform.TestData) | ✅ available |
| UI layer (Selenium + POM) | [src/QaPlatform.Ui](src/QaPlatform.Ui) | ✅ available |
| API tests (xUnit + WireMock.Net) | [tests/QaPlatform.ApiTests](tests/QaPlatform.ApiTests) | ✅ available |
| UI tests (xUnit + Selenium 4, skipped by default) | [tests/QaPlatform.UiTests](tests/QaPlatform.UiTests) | ✅ available |
| Local stack (Docker Compose) | [infra/docker-compose.test.yml](infra/docker-compose.test.yml) | ✅ available |
| Test runner image | [infra/Dockerfile.test-runner](infra/Dockerfile.test-runner) | ✅ available |
| CI pipeline (Azure DevOps) | [infra/azure-pipelines.yml](infra/azure-pipelines.yml) | ✅ available |
| Performance (JMeter) | [performance/](performance/) | ✅ available |
| Walkthroughs (narrative) | [walkthroughs/](walkthroughs/) | ✅ available |

Each section of this README will be expanded as the corresponding project lands.

---

## Prerequisites

You need exactly one thing today:

- **.NET 8 SDK** (verified against `8.0.418`). Any 8.0.x SDK should work.
  - macOS: `brew install --cask dotnet-sdk`
  - Windows: <https://dotnet.microsoft.com/download/dotnet/8.0>
  - Linux: see the link above for distro packages

Verify:

```bash
dotnet --version
# 8.0.x
```

Future additions to this folder will introduce optional dependencies (Docker Desktop for the local stack, a Java runtime for JMeter). They are not required to build or test what's here today.

### Why .NET 8 and not .NET Framework 4.x

The case study these samples document was originally built on the .NET Framework toolchain of the time. The samples target **.NET 8** instead so that anyone reading this in 2026 can build and run them with a single SDK install. The *patterns* are period-faithful; the *runtime* is current.

---

## Repository layout

```
code-samples/
├── QaPlatform.Samples.sln          # solution; add all projects here
├── src/
│   ├── QaPlatform.Core/            # configuration, logging, stability primitives
│   ├── QaPlatform.ApiClient/       # typed client, payload builders, assertion extensions
│   ├── QaPlatform.TestData/        # builders + cleanup registry (LIFO, fail-safe)
│   └── QaPlatform.Ui/              # POM base + selectors-per-page + concrete pages
├── tests/
│   ├── QaPlatform.ApiTests/        # xUnit + WireMock.Net — runs on a clean clone
│   └── QaPlatform.UiTests/         # xUnit + Selenium 4, skipped by default
├── infra/
│   ├── Dockerfile.test-runner      # parameterised single image; sharded at runtime
│   ├── docker-compose.test.yml     # Selenium Grid + fake API + runner
│   ├── run-shard.sh                # entrypoint: picks the right slice of tests
│   └── azure-pipelines.yml         # build → sharded API → UI → RC gate
├── performance/
│   └── ran-onboarding-baseline.jmx # JMeter baseline; see performance/README.md
├── walkthroughs/                   # numbered narrative for hiring managers / SDETs
│   ├── 01-pom-base-class/
│   ├── 02-api-client-wrapper/
│   ├── 03-fixture-builder/
│   ├── 04-full-e2e-test/
│   └── 05-ci-yaml/
└── README.md                       # this file
```

The full target layout is sketched in the **Status** table above.

---

## Build

From this folder (`code-samples/`):

```bash
# Restore + build everything in the solution
dotnet build

# Build a single project
dotnet build src/QaPlatform.Core/QaPlatform.Core.csproj
```

A clean build should produce **0 warnings, 0 errors**. If you see warnings, treat them as build failures — these samples are meant to model the discipline they describe.

---

## Run the tests

```bash
# Run all tests across the solution
dotnet test

# Run a single test project
dotnet test tests/QaPlatform.ApiTests/QaPlatform.ApiTests.csproj

# Run a single test by fully-qualified name
dotnet test --filter "FullyQualifiedName~OperatorOnboarding"
```

The API tests run against an in-process [WireMock.Net](https://github.com/WireMock-Net/WireMock.Net) fake — no network, no Docker, no external services. A clean clone should produce:

```
Passed!  - Failed: 0, Passed: 3, Skipped: 0, Total: 3
```

UI tests are tagged with `[Trait("Category", "UI")]` and **skipped by default** — they require a Selenium endpoint and a running UI under test. Enable them by setting:

```bash
export QA_UI_BASEURL=https://app.dev.example.internal
# optional — use a remote Selenium Grid instead of local Chrome:
export QA_SELENIUM_URL=http://selenium-grid:4444/wd/hub
dotnet test --filter "Category=UI"
```

---

## Configuration model

Tests in this repo never read environment variables directly. They go through `EnvironmentConfig`, which resolves in this order (highest precedence first):

1. Process environment variables of the form `QA_<KEY>` (e.g. `QA_APIBASEURL`)
2. Per-environment defaults baked into [`EnvironmentConfig`](src/QaPlatform.Core/Configuration/EnvironmentConfig.cs) for `Local`, `Dev`, `Staging`
3. Throw — unknown keys are a hard failure, not a silent default

The active environment is selected by `QA_ENVIRONMENT` (`Local` / `Dev` / `Staging`). It defaults to `Local`.

### Example: pointing tests at a non-default API base URL

```bash
export QA_ENVIRONMENT=Dev
export QA_APIBASEURL=https://api.my-feature-branch.internal
dotnet test
```

### Why this design

A test that passed in `Dev` must run, unchanged, against `Staging`. Environment differences live in configuration, never in test code branches. The discipline is documented in [/architecture/system-architecture.md](../architecture/system-architecture.md#3-tests--environments).

---

## End-to-end with Docker (optional)

Everything above runs without Docker. If you want the full stack — Selenium Grid, a fake API, the test runner image — bring it up with Compose:

```bash
docker compose -f infra/docker-compose.test.yml up --build --abort-on-container-exit
```

What you get:

| Service | Purpose | Port |
|---|---|---|
| `selenium-hub` | Selenium 4 Grid hub | 4444 |
| `chrome` | Chrome node attached to the hub | (internal) |
| `fake-api` | Standalone WireMock — stands in for the product API | 8080 |
| `test-runner` | Built from [Dockerfile.test-runner](infra/Dockerfile.test-runner); runs one shard | n/a |

The runner uses [run-shard.sh](infra/run-shard.sh) as its entrypoint — the same script CI uses, parameterised by `SHARD_INDEX` / `SHARD_TOTAL`. Mirrors the per-shard isolation discipline in [/architecture/environments.md](../architecture/environments.md#2-isolated-per-shard).

---

## CI pipeline

[infra/azure-pipelines.yml](infra/azure-pipelines.yml) is a working illustration of the flow described in [/architecture/test-execution-flow.md](../architecture/test-execution-flow.md):

1. **Build** — warnings are errors (`/warnaserror`).
2. **API / integration** — fanned out across **4 parallel shards**.
3. **UI** — Chrome + Firefox in a matrix, each backed by a `selenium/standalone-*` service container.
4. **Release-candidate gate** — main-branch-only, deploys to the `staging` Azure DevOps environment (which is where approvals are configured, not in YAML).

The pipeline is structural reference — it won't run as-is without a target environment configured in your Azure DevOps project.

---

## Conventions used throughout these samples

These are the rules the code in this folder follows, not aspirations:

- **No `Thread.Sleep` in tests.** Use [`Wait.Until`](src/QaPlatform.Core/Stability/Wait.cs) or `Wait.For`. Sleeps are a PR-block offence; see [/architecture/system-architecture.md](../architecture/system-architecture.md#ui-layer-selenium--pom).
- **Retry is opt-in, idempotent-only.** Use [`IdempotentRetry`](src/QaPlatform.Core/Stability/IdempotentRetry.cs) with an explicit `RetryPolicy`. There is no generic "retry anything" helper, by design.
- **Configuration is injected, never hard-coded.** Read everything through `EnvironmentConfig`.
- **One structured log line per event.** Use [`TestLogger`](src/QaPlatform.Core/Logging/TestLogger.cs) — JSON-per-line so parallel shards stay parseable.
- **Selectors live in one file per page** (once the UI layer lands). A UI refactor should be a one-file diff.
- **Each test owns its data and cleans up.** No shared mutable state. The cleanup-on-failure path is as important as the happy path.

---

## Troubleshooting

**`dotnet: command not found`** — Install the .NET 8 SDK (see Prerequisites).

**Build succeeds but tests don't appear in your IDE** — Run `dotnet restore` once at the solution root, then reload the IDE's solution view.

**`KeyNotFoundException: Configuration key '...' has no value`** — You read a config key that isn't in the defaults for the active environment and isn't set as `QA_<KEY>`. Either set the env var or add a default for that environment in `EnvironmentConfig`.

**`TimeoutException: Timed out after Xs waiting for: ...`** — A `Wait.Until` deadline expired. The exception message includes the description and the last exception observed during polling — use those to diagnose. Do not increase the timeout reflexively.

---

## See also

- [/architecture](../architecture/) — the design these samples implement
- [/architecture/testing-pyramid.md](../architecture/testing-pyramid.md) — why the test mix is shaped the way it is
- [/architecture/test-execution-flow.md](../architecture/test-execution-flow.md) — how these tests would run in CI
