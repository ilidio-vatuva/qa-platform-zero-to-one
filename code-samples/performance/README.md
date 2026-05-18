# Performance

> A single illustrative JMeter baseline scenario, kept structurally separate from the functional suite.

This folder is small on purpose. The architecture chose to keep performance testing **out of the per-commit pipeline** (different cadence, different gates, different audience — see [/architecture/system-architecture.md](../architecture/system-architecture.md#performance-jmeter)). What's here is the *shape* of a working JMeter plan, not a complete performance harness.

---

## Contents

| File | What it does |
|---|---|
| [ran-onboarding-baseline.jmx](ran-onboarding-baseline.jmx) | Submit-and-poll baseline for network-element onboarding. 20 virtual users, 60s ramp, 5-minute steady run. Asserts HTTP 202 on submit + p95 latency under 800 ms. |

---

## Run it

Prerequisites: [Apache JMeter 5.6+](https://jmeter.apache.org/) and a Java 11+ runtime.

```bash
mkdir -p performance/results
jmeter -n \
  -t performance/ran-onboarding-baseline.jmx \
  -Jhost=api.staging.example.internal -Jport=443 -Jscheme=https \
  -l performance/results/baseline.jtl
```

Against a local fake (e.g. the WireMock service in [infra/docker-compose.test.yml](../infra/docker-compose.test.yml)):

```bash
jmeter -n -t performance/ran-onboarding-baseline.jmx \
  -Jhost=localhost -Jport=8080 -Jscheme=http \
  -l performance/results/baseline-local.jtl
```

---

## What this scenario is, and isn't

- **Is** a *baseline* — establishes "what good looks like" under modest, realistic load.
- **Isn't** a soak test (24h+), a spike test (sudden 10× burst), or a vendor-mix scalability test. Those belong in **sibling .jmx files**, each with one job. Resist the temptation to put everything in one plan; it makes failures ambiguous.

---

## How this would fit a real pipeline

- Run nightly against staging, against the most recent main-branch artefact.
- Latency and throughput numbers feed a small SRE-owned dashboard, not the per-commit gate.
- A *regression* against last week's baseline (e.g. p95 worsens by >20%) opens a ticket, but doesn't block releases automatically — the team learned the hard way that auto-blocking on noisy perf metrics destroys trust.

See [/architecture/test-execution-flow.md](../architecture/test-execution-flow.md) for how this fits the broader release flow.
