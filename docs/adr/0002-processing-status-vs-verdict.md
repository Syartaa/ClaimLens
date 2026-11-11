# ADR 0002 — Processing status is not the verdict

## Status

Accepted.

## Context

The brief used `Investigation.Status` for values like “gathering evidence” and also listed product statuses such as “Mixed Evidence”. Those are different lifecycles. A run can finish in a state of insufficient evidence, and a failed run should not be forced into a verdict.

## Decision

- `ProcessingStatus` tracks the worker.
- `Verdict` is null until the calculator finishes.
- `Failed` is a processing status. `FailureCode` holds a stable code such as `AI_RESPONSE_INVALID` or `EVIDENCE_PROVIDER_FAILED`.
- The UI badge uses the verdict when processing is `Completed`, and the processing label otherwise.

## Consequences

Filters on the history page can query either field. Dashboard “completed” means `ProcessingStatus = Completed`, not a particular verdict.
