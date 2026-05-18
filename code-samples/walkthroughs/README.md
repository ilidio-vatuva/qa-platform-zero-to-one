# Walkthroughs

> The same code as [/code-samples](..), told as a numbered story for a hiring manager or technical interviewer.

Each walkthrough is short (5–10 minutes to read), grounded in real files in the repo, and answers the question *"why was it done this way?"* — not just *"what does it do?"*

Read them in order on a first pass. Each later walkthrough builds on the patterns established in the earlier ones.

---

## Reading order

1. **[POM Base Class](./01-pom-base-class/README.md)** — intent vs. selectors, explicit waits, why `Thread.Sleep` was a PR-block offence.
2. **[API Client Wrapper](./02-api-client-wrapper/README.md)** — typed client per domain, builders, co-located assertions.
3. **[Fixture Builder](./03-fixture-builder/README.md)** — builder pattern with LIFO cleanup that survives partial failures.
4. **[Full E2E Test](./04-full-e2e-test/README.md)** — the moment the three previous patterns compose into a readable end-to-end test.
5. **[CI YAML](./05-ci-yaml/README.md)** — sharded parallel stages, blocking gates, why the YAML enforces the contract.

---

## How to use these

- **Recruiter / hiring manager:** read 1 and 4. They tell the story.
- **Engineering manager preparing the technical interview:** read 1 → 5 in order; the *why* sections in each are interview discussion fodder.
- **Peer SDET reviewing the approach:** start at the real code in [/code-samples/src](../src) and use these as a navigation index.

Each walkthrough links directly into the real files. Nothing here is paraphrased — if a walkthrough disagrees with the code, the code wins.
