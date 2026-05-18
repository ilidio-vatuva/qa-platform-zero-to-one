# ADR-0003: Staging carries release-candidate duty (no pre-prod)

- **Status:** Accepted
- **Date:** 2020-Q3
- **Deciders:** QA platform lead, engineering manager, head of platform

---

## Context

A "proper" enterprise SaaS shop has four environment tiers: local, dev, staging, **pre-prod**, prod. Pre-prod is a customer-shaped clone — same data volumes, same vendor integrations, same network topology — where release candidates bake for 24–72 hours before going to a Tier 1 operator.

We had four tiers minus one. **There was no pre-prod.**

Standing one up would have required:
- A second copy of every vendor simulator and (where simulators didn't exist) a second hardware lab.
- Production-scale data sets, synthesised or anonymised, with the privacy review that implies.
- An additional deployment target in every pipeline, every dashboard, every alert.
- Roughly one full-time SRE to keep it healthy.

The budget for that conversation did not exist in 2020. The platform was earning its right to exist quarter by quarter.

## Decision

**Staging carries double duty as both the integration environment and the release-candidate gate.** No pre-prod tier.

We compensated by:

1. **Pinning a "release-candidate window" on staging.** For 48 hours before a customer release, staging is frozen — no new feature branches deploy. Only the RC build runs. Performance tests, soak tests, and the full UI regression suite all run against this frozen RC.
2. **Vendor simulators tuned to production-like volumes** (see [ADR-0006](0006-vendor-simulators-over-hardware-lab.md)). Not a real customer environment, but closer than dev.
3. **Canary deployment to one small Tier 2 operator first**, with a 72-hour observation window before rolling to Tier 1s. Production *is* the pre-prod for the Tier 1s. This is honest about what we were doing.

## Consequences

**Good**
- We shipped. A two-year wait for "the right environment topology" would have killed the platform's momentum.
- The discipline forced into the simulator strategy (ADR-0006) turned out to be more valuable than a hardware pre-prod would have been — simulators are reproducible, hardware labs drift.
- The canary-to-Tier-2 step caught real issues twice during the case-study window. Issues that a pre-prod *might* have caught, but probably wouldn't, because pre-prods almost never have real vendor firmware quirks.

**Bad**
- Two production incidents during the case-study window were directly attributable to the missing tier — both were data-volume issues that staging's simulators didn't reproduce. A pre-prod with production-scale data would have caught them.
- The on-call rotation absorbed the cost. Engineers were paged for things a pre-prod would have surfaced during business hours. That burnout cost is real and was not in the original budget conversation.
- "Why is there no pre-prod?" became a recurring question in customer security reviews. The honest answer ("budget and headcount") is uncomfortable in a sales context.

**Neutral**
- The "release-candidate window" turned staging into a part-time environment. Feature teams learned to plan around the 48-hour freezes. Annoying, not catastrophic.

## The honest line

> **Building a customer-shaped pre-prod was on the roadmap when this case study ends. It was not delivered.**

Anyone reading this ADR in an interview context should ask about that, and the honest answer is: the case for it was made, the budget was repeatedly deferred, and the team made the most of what existed. That is the truth of working in resource-constrained platform teams, and pretending otherwise would falsify the rest of this repo.

## Alternatives considered

- **Spin up a pre-prod anyway, on shoestring infrastructure.** Considered. Rejected because a half-built pre-prod is worse than none — it creates false confidence. If we couldn't get the data volumes and vendor coverage right, the tier would lie to us.
- **Use a customer's own pre-prod environment.** Politically impossible; customers were not going to lend us their environments.
- **Skip staging entirely and beef up dev.** Rejected: would have collapsed the integration-testing gate into the development gate, which is the gate developers most want to bypass.

## Related

- [architecture/environments.md](../architecture/environments.md) — the four tiers and the "Living without a pre-prod" section
- [architecture/test-execution-flow.md](../architecture/test-execution-flow.md) — where the RC validation slots into the pipeline
- [ADR-0006](0006-vendor-simulators-over-hardware-lab.md) — the strategy that made this survivable
- [lessons-learned.md](../lessons-learned.md) — the incidents and what they cost
