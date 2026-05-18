# ADR-0004: Structured JSON logging from day one

- **Status:** Accepted
- **Date:** 2020-Q2
- **Deciders:** QA platform lead

---

## Context

The default for test logging is `Console.WriteLine` (or `print`, or `System.out.println`). It's easy, it works locally, and it's the path of least resistance.

It also collapses the moment you go parallel. Once you have 4 shards × 50 tests running concurrently, interleaved free-form text logs are unreadable. You spend more time grepping `aaa-bbb-ccc` correlation IDs out of timestamp soup than you spend diagnosing the actual failure.

The platform was committed to parallel execution (see [ADR-0001](0001-api-heavy-pyramid.md)) and to running in containerised CI agents where the only diagnostic artifact was the log stream. Free-form logging would have been a quiet productivity tax forever.

## Decision

**All test-framework logging is structured JSON, one event per line.**

Every log line has at minimum:

```json
{
  "ts": "2020-07-14T11:32:18.422Z",
  "level": "Info",
  "test": "NetworkElementOnboardingTests.Onboarding_succeeds_when_provisioning_returns_ready",
  "shard": "api-2",
  "event": "cleanup.registered",
  "resource": "network-element/abc-123"
}
```

Implemented as a single `TestLogger` class with a fixed JSON serialiser. No `Console.WriteLine` is allowed in framework code. (Test bodies can still use it — we're not religious about it — but the cleanup, retry, and HTTP layers all go through `TestLogger`.)

See [Logging/TestLogger.cs](../code-samples/src/QaPlatform.Core/Logging/TestLogger.cs).

## Consequences

**Good**
- CI logs are grep-friendly *and* tool-friendly. `jq 'select(.event == "cleanup.failed")'` is a one-liner that would be impossible against free-form text.
- The cleanup-order test in [walkthroughs/03-fixture-builder](../code-samples/walkthroughs/03-fixture-builder/) asserts against JSON log events directly. That test would not exist with free-form logs.
- When we wired up centralised log aggregation in Phase 2, the test logs slotted in alongside production logs without a parser. The on-call dashboard could show "test failures in the last hour" with the same query language as production errors.

**Bad**
- Reading raw JSON logs locally is uglier than reading text. We mitigated with a small `jq` wrapper script, but new engineers complain for the first week.
- The discipline has to be enforced. Every PR that adds a `Console.WriteLine` to framework code gets reverted. Without that vigilance, the format erodes.
- Some downstream tools (older test-result viewers) didn't parse structured logs and showed them as a blob. Not a dealbreaker but annoying.

**Neutral**
- The schema (`ts`, `level`, `test`, `shard`, `event`, payload) was set on day one and has barely changed. That stability is the whole point, but it does mean adding a new top-level field requires real thought.

## Alternatives considered

- **`Console.WriteLine` (default).** Rejected: collapses under parallel execution. Discussed at length above.
- **A logging framework (Serilog, NLog).** Considered. Rejected at the time because: (a) the test framework was already a heavy assembly, (b) we only needed two sinks (stdout, file), (c) a 50-line `TestLogger` was easier to debug than a configured Serilog pipeline. In a larger project, Serilog with its `CompactJsonFormatter` would be the obvious choice and there'd be no shame in it.
- **Per-test log files.** Rejected: parallel tests don't know which file they belong to without coordination, and the artifact upload from CI gets unwieldy fast.

## Related

- [code-samples/src/QaPlatform.Core/Logging/TestLogger.cs](../code-samples/src/QaPlatform.Core/Logging/TestLogger.cs) — the implementation
- [code-samples/walkthroughs/03-fixture-builder/](../code-samples/walkthroughs/03-fixture-builder/) — uses JSON log lines as test assertions
- [ADR-0001](0001-api-heavy-pyramid.md) — the parallel-execution commitment that made this necessary
