# ADR-0006: Vendor simulators over a shared hardware lab

- **Status:** Accepted
- **Date:** 2020-Q3
- **Deciders:** QA platform lead, head of platform, RAN domain lead

---

## Context

Telecom RAN automation has an inconvenient truth: most of the system under test is *someone else's hardware*. Base stations from Ericsson, Nokia, Huawei, Samsung. Each vendor speaks slightly different dialects of standardised protocols (NETCONF, SNMP, vendor-specific REST). Each has firmware quirks. Each behaves differently under load.

The traditional industry answer is a **hardware lab**: real equipment, in racks, on a private network, shared across QA and engineering. Most large telecom vendors and operators have one.

A hardware lab has properties we did not want to accept:

- **Shared = serialised.** Two test runs can't safely talk to the same base station. The lab becomes a booking-system bottleneck.
- **Drift.** Firmware versions, cabling, configuration — they all drift. "It worked yesterday" becomes a permanent condition.
- **Cost.** A representative multi-vendor lab is mid-six figures of capital plus a full-time operator.
- **Not in CI.** You cannot, realistically, attach a hardware lab to a per-PR pipeline.

## Decision

**Vendor simulators are the default test substrate. Hardware lab access is reserved for specific, scheduled validation.**

Concretely:

- For every vendor protocol we automate, we maintain a **simulator** — either vendor-provided where one exists, or a homegrown HTTP/NETCONF fake that conforms to the same schema. The code samples in this repo use [WireMock](https://github.com/WireMock-Net/WireMock.Net) as the stand-in pattern (see [docker-compose.test.yml](../code-samples/infra/docker-compose.test.yml)).
- API and UI tests run against simulators. Always. In CI and locally.
- A separate **hardware-validation suite** runs weekly against the real lab. It covers firmware-specific behaviours, timing edge cases, and "did the vendor change the protocol again" canaries.
- New vendor integrations start with a simulator. The simulator and the integration code are written in parallel, often by the same engineer. The hardware comes later.

## Consequences

**Good**
- CI runs in minutes against simulators. The same scenarios against a hardware lab would take hours and require booking.
- Test environments are reproducible. `docker compose up` gives every engineer the same multi-vendor stack. Drift is impossible because the simulators are versioned with the code.
- Parallel execution becomes real. Each test shard gets its own simulator instance. No booking, no serialisation.
- When the hardware lab finds a bug the simulator missed, the simulator gets updated. Over time the simulator becomes the *better* representation, because every bug is a permanent improvement to it.

**Bad**
- Simulators lie. They lie about timing, they lie about partial failures, they lie about vendor-specific edge cases. The weekly hardware run catches some of this; production catches the rest. We accept this and document it.
- Building and maintaining simulators is a real engineering cost — roughly 15–20% of the platform team's time. Stakeholders periodically question this. The answer is always the same: the alternative is a lab whose maintenance cost is higher *and* invisible.
- A small number of bugs are "simulator-only" — artifacts of the fake, not the real vendor. We learned to treat any test failure that only reproduces against the simulator as suspect until proven otherwise.

**Neutral**
- The simulators are not open-sourced. They encode enough vendor protocol detail that releasing them would raise IP questions we didn't want to litigate.

## Alternatives considered

- **Hardware lab as primary.** Rejected for the reasons listed at the top: serialised, drifty, expensive, can't go in CI.
- **Cloud-based vendor labs (where vendors offered them).** Considered. Used opportunistically for one vendor. Rejected as a primary strategy because not every vendor offered one, and the ones that did had usage quotas incompatible with per-PR testing.
- **Recorded-and-replayed real traffic (VCR-style).** Considered for read-heavy scenarios. Rejected for write-heavy ones (onboarding, configuration push) because the recordings encode timing assumptions that break the moment the test changes.

## Related

- [architecture/environments.md](../architecture/environments.md) — where simulators live in the environment model
- [code-samples/infra/docker-compose.test.yml](../code-samples/infra/docker-compose.test.yml) — WireMock as the simulator stand-in
- [code-samples/tests/QaPlatform.ApiTests/Fakes/FakeNetworkElementsService.cs](../code-samples/tests/QaPlatform.ApiTests/Fakes/FakeNetworkElementsService.cs) — a simulator in miniature
- [ADR-0003](0003-no-pre-prod-environment.md) — the environment ADR that made this strategy load-bearing
