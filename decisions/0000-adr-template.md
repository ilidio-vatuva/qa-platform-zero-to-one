# ADR-0000: ADR template & process

- **Status:** Accepted
- **Date:** 2020-Q2
- **Deciders:** QA platform lead, engineering manager

---

## Context

The QA platform was about to make a series of opinionated, hard-to-reverse decisions: testing-pyramid shape, language choice, CI strategy, environment model. Decisions made in chat or in slide decks tend to evaporate. Six months later, nobody remembers *why* a thing was done, only *that* it was done — and the next engineer breaks it trying to "improve" it.

We needed a lightweight, durable format for capturing decisions.

## Decision

Adopt a stripped-down [Michael Nygard ADR format](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions). One file per decision. Numbered sequentially. Stored in `/decisions/` at the repo root.

Each ADR has exactly these sections:

- **Status** — `Proposed` / `Accepted` / `Superseded by ADR-XXXX` / `Deprecated`
- **Date** — when it was accepted (or proposed)
- **Deciders** — roles, not names
- **Context** — the forcing function. What problem are we choosing between solutions for?
- **Decision** — what we're going to do. Active voice, present tense.
- **Consequences** — three buckets: **Good**, **Bad**, **Neutral**. All three must be filled in. If you can't name a downside, you haven't thought hard enough.
- **Alternatives considered** — what we rejected and why. One paragraph each.
- **Related** — links to other ADRs, architecture docs, or code.

## Rules

1. **ADRs are immutable once `Accepted`.** Typos and broken links can be fixed. The decision itself cannot. If the decision changes, write a new ADR that supersedes the old one.
2. **Supersede, don't delete.** A `Superseded` ADR stays in the repo. Reading the chain `0002 → 0008` is how new engineers learn the platform's evolution.
3. **One decision per ADR.** If you find yourself writing "and also…", that's a second ADR.
4. **Short.** If it doesn't fit on one screen at 14pt, it's probably two decisions.
5. **No implementation details.** Link to code; don't embed it.

## Consequences

**Good**
- New engineers can read `/decisions/` in an afternoon and understand *why* the platform looks the way it does.
- Decisions are debatable on their merits, not on whoever remembers the meeting.
- "We tried that already and it didn't work" becomes a citation, not a vibe.

**Bad**
- Writing an ADR takes 30–60 minutes. Some decisions get made without one because the author was in a hurry. The ones that get skipped are usually the ones we later regret most.
- ADRs can become a process-theatre tool if reviewers fixate on format over substance.

**Neutral**
- We commit to maintaining this folder. If it bit-rots, it's worse than not having it — a stale ADR actively misleads.

## Alternatives considered

- **Confluence pages.** Rejected: not versioned with the code, no review workflow, dies the day someone leaves the company.
- **Long-form RFCs in a separate repo.** Rejected: too heavy for the cadence we needed. RFCs tend to gate decisions; ADRs document them.
- **Inline code comments.** Rejected: doesn't survive refactors, and decisions usually span multiple files.

## Related

- All other ADRs in this folder use this template.
