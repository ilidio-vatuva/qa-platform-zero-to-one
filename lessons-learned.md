# Lessons Learned

> What the case study taught me, in the voice of someone who'd want to know it before starting.

The root [README.md](README.md) lists five headline lessons. This file is the longer version — what each one *actually* meant in practice, and what I'd tell someone starting a similar effort tomorrow.

These are ordered roughly by how much they would have helped if I'd known them on day one.

---

## 1. Culture beats tools. By a lot.

**The lesson:** You can ship a beautiful test framework into a culture that doesn't want it, and within six months it will be a museum exhibit nobody touches.

**What it looked like in practice:** Phase 1 had a working Selenium + POM framework before it had a single developer who'd contributed to it. The tests were "the QA team's tests." When a test broke, the default Slack response was *"hey QA, your test is red"* — even when the red was a real product regression.

The shift only started when:

- Test failures showed up on the *author's* PR page with their name on it (see [ADR-0005](decisions/0005-tests-as-blocking-gates.md)).
- A senior backend developer, on his own initiative, added an API test for a bug he'd just fixed. Once one of them did it, the wall broke.
- We stopped saying "the QA tests" in standups. They became "the tests." Same suite, different sentence.

**What I'd tell someone starting:** Spend the first month winning two non-QA engineers over. Not training them. Pairing with them, fixing their flake, making their PR experience better. The framework can wait one month. The cultural foothold cannot.

---

## 2. A flaky test is worse than no test.

**The lesson:** Flake is not a maintenance problem. It is a *trust* problem. The moment the team learns that some red builds are "probably the flaky one", they stop reading red builds at all — including the ones that are real regressions.

**What it looked like in practice:** Phase 1's flake rate climbed to ~12% before we noticed. By the time we noticed, the team had already developed the habit of retrying failed builds without reading the logs. We measured this — the time between a red build and the next push of the same branch was under 90 seconds in 60% of cases. That's not "investigating", that's "hoping".

The fix was unglamorous: a stability sprint, the quarantine policy (see [test-execution-flow.md](architecture/test-execution-flow.md)), and a standing rule that flake rate over 2% on the rolling 7-day window triggered the next stability sprint. No exceptions, no "we're busy this quarter".

**What I'd tell someone starting:** Pick a flake-rate threshold on day one. Defend it ruthlessly. The day you negotiate it down is the day the suite starts dying.

---

## 3. Automation isn't binary.

**The lesson:** Some testing should stay manual forever, and the team that admits this ships faster than the team that doesn't.

**What it looked like in practice:** We had a quarterly conversation about which categories of testing would *never* be automated:

| Category | Why it stayed manual |
|---|---|
| Vendor-firmware quirks | Each vendor's edge cases were too specific and changed too often to write durable tests for |
| Operator-facing wizard rehearsals | Customers wanted a human in the loop for go-live anyway |
| First-pass exploration of new features | A human asks better questions than a regression suite |
| Visual regression of dense data dashboards | Pixel-diff tools generated more noise than signal |

The fraction of testing that *stayed* manual was small (~5–8% of total effort) but real, and pretending otherwise would have meant either (a) shipping a brittle suite of pixel-diff tests, or (b) skipping that testing entirely.

**What I'd tell someone starting:** Write down what you've decided *not* to automate. Review it quarterly. Things on the list move off when they become stable; things off the list move on when they become noisy. The list is the artefact.

---

## 4. Design for testability from the start, or pay forever.

**The lesson:** Hard-to-test code is usually also hard-to-use code. By the time you're writing the test, the cost of fixing the underlying design is 10× what it would have been at the PR review for the production code.

**What it looked like in practice:** The biggest source of UI test pain was a single onboarding wizard that surfaced its progress through `console.log` instead of a visible loading state. Every test that touched it needed bespoke `Wait.Until` logic and screen-scraping. We tried to automate around it for three months. Eventually the frontend team added a `data-onboarding-state` attribute — a 1-line change that retired 200 lines of test scaffolding.

The same pattern showed up at the API layer: endpoints that returned `200 OK` with an embedded `{"status": "failed"}` instead of a non-2xx status. Each one took a test author by surprise once.

**What I'd tell someone starting:** Sit in the API design reviews. Sit in the UI component reviews. The smallest QA-time investment in the design phase pays back enormous test-time savings later. The two questions to ask in every review: *"How will I know this succeeded?"* and *"How will I know this failed without polling?"*.

---

## 5. The pipeline is the only honest contract.

**The lesson:** Any quality gate that depends on a human's judgment is a gate that gets bypassed under pressure. The pipeline is the only place where "we don't ship if X is red" stays true on a Friday at 17:55.

**What it looked like in practice:** Before the blocking gates (see [ADR-0005](decisions/0005-tests-as-blocking-gates.md)), the release-go/no-go conversation was 45 minutes of negotiation. After the gates, it was 5 minutes — because there was nothing to negotiate. Either staging was green or it wasn't. Either the QA pipeline had verdicts or it didn't.

The two-pipeline split (dev CI/CD owned by devs, QA pipeline owned by QA) wasn't elegant. It was the org-chart-shaped solution that was actually shippable. The cleaner "one pipeline to rule them all" design would have required a team that didn't exist and a political agreement nobody was going to make.

**What I'd tell someone starting:** Match your pipeline split to your org chart. Then write down which gate blocks which decision. Print it. Pin it. When someone tries to override one, point at the print-out.

---

## 6. Honest constraints beat aspirational architecture.

**The lesson:** We didn't have a pre-prod environment. We didn't pretend we did. The architecture worked because every doc, every ADR, every CI stage was honest about that gap — and the canary deployment to a small operator was the explicit, acknowledged absorption point for the risk that gap created.

**What it looked like in practice:** See [ADR-0003](decisions/0003-no-pre-prod-environment.md). The honest line — *"Building a customer-shaped pre-prod was on the roadmap when this case study ends. It was not delivered."* — is the most-quoted sentence in the whole repo when I show it to other engineers, and it's quoted *positively*. Engineers respect honest constraints. They roll their eyes at architectures that pretend constraints don't exist.

**What I'd tell someone starting:** Write down what you don't have. In the architecture doc, not in a buried retro. The reader you're trying to impress is the one who's worked in a constrained environment before — they'll trust honest gaps and distrust seamless stories.

---

## 7. Metrics measure the wrong thing by default.

**The lesson:** Coverage %, test count, and pass rate are the metrics that get into slide decks. *None of them* tell you whether the suite is doing its job.

**What it looked like in practice:** We watched four metrics instead:

| Metric | Why it actually matters |
|---|---|
| **Flake rate (rolling 7-day)** | The leading indicator of suite trust collapsing |
| **Time-to-green after a red main** | How long the team can ship anything after a regression |
| **Median dev CI/CD wall-clock** | If this crosses ~5 min, devs start asking to weaken gates |
| **Escaped defects per release** | The only metric that says *did this suite actually catch bugs* |

We *also* tracked coverage %, but only as a debugging tool — "the new module dropped 30 points, why?" — never as a target.

**What I'd tell someone starting:** Pick the metric that, if it goes wrong, kills the whole effort. Watch that one. The others are diagnostic.

---

## 8. Vendor simulators repay their cost 10× — but you have to commit.

**The lesson:** Building and maintaining a vendor simulator is a real cost (15–20% of platform-team time, see [ADR-0006](decisions/0006-vendor-simulators-over-hardware-lab.md)). Every quarter someone asks if it's worth it. The answer is always yes, and the proof is that we never had a "the lab is booked, we can't test" conversation again after Phase 2.

**What it looked like in practice:** The first time the simulator was deliberately wrong (we'd hard-coded a response that the real vendor didn't return), it took a team-on-call to find out. We added a weekly "simulator drift check" — same test, real lab, compare results — and from then on every drift was a permanent simulator improvement.

**What I'd tell someone starting:** The simulator is a product. Treat its bugs as product bugs, not as test infrastructure annoyances. Version it. Code-review it. Test it.

---

## 9. Cleanup before the operation, every time.

**The lesson:** Register the cleanup *before* you call the create. If the create fails halfway, your cleanup still runs against the partial state and the test environment stays clean.

**What it looked like in practice:** See [ADR-0007](decisions/0007-cleanup-before-operation.md) and the [03 walkthrough](code-samples/walkthroughs/03-fixture-builder/). Before this discipline: ~40 orphaned resources per week in staging, with a nightly sweeper trying to catch up. After: zero, across the entire case-study window.

**What I'd tell someone starting:** This is the smallest pattern in the repo with the biggest payoff. It feels backwards the first three times you write it. After that you write tests in any other order and feel uneasy.

---

## 10. You will be wrong about something important. Plan for the supersession.

**The lesson:** The 2020 stack choice (C# + Selenium) was right *in 2020*. It is not right in 2026. The portfolio is more credible *because* it has an ADR that admits this (see [ADR-0002](decisions/0002-csharp-selenium-stack.md) → [ADR-0008](decisions/0008-revisiting-ui-layer-playwright.md)) than it would be if it pretended the original choice was timelessly correct.

**What it looked like in practice:** The hardest part of writing this repo years later wasn't the technical recall. It was deciding which of the original decisions to defend and which to mark `Superseded`. The temptation is to defend everything, because admitting a wrong call feels like a weakness. It is in fact the opposite — the only architectures that age well are the ones with visible evolution.

**What I'd tell someone starting:** Date your decisions. Number them. When one turns out to be wrong, write the new one, mark the old one `Superseded`, and link them. Future-you will thank present-you for the trail.

---

## What this file is not

- **Not a retrospective.** Retrospectives are about a specific quarter or incident. These are durable lessons that outlive the case study.
- **Not a substitute for the ADRs.** ADRs record *decisions*. This file records what the *decisions taught me*. They're different artefacts.
- **Not exhaustive.** There are dozens of smaller lessons (some are in the ADRs' "Consequences" sections, some are in the walkthroughs). These ten are the ones I'd put on the wall.
