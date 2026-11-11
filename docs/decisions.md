# Decisions and simplifications

These are deliberate changes to the specification. They should be treated as agreed unless we revise them before Phase 2.

## Split status and verdict

`Investigation.Status` in the brief covered both pipeline progress and the evidence outcome. Those are `ProcessingStatus` and `Verdict`. A completed investigation can be `InsufficientEvidence`. A failed run has no verdict.

## The model never owns the verdict

`AnalysisResult` records the calculator version, not an LLM score. Model name and prompt version live on `AiRun`. Summary text may come from the model, but only after the verdict exists, and the prompt receives the verdict as an input it must not change.

Relevance cutoffs, confidence cutoffs, source-quality weights, and verdict boundaries are engineering heuristics. They live in `VerdictThresholds`, stay deterministic, and are not described as scientifically validated probabilities or as the probability that a claim is true. Unit tests cover them when the calculator is built in Phase 8.

## PostgreSQL is the MVP queue

Hangfire, Redis, and SignalR are postponed. A worker claims `Pending` investigations with `FOR UPDATE SKIP LOCKED`. That survives a process restart without a second datastore. See [ADR 0003](adr/0003-postgres-backed-jobs.md).

## No MediatR

Application services are easier to follow in a portfolio repository of this size. Add a mediator only if a single use case grows multiple independent side effects that need their own pipeline.

## Shared sources

Sources are unique by URL and reused across claims. Evidence rows point at a source. Deleting an investigation deletes evidence, not the source, when another investigation still references it. The cascade rules in the database doc match that.

## Source quality is a band of indicators

There is no numeric “credibility” that could be read as a fact-check of the publisher. The band is `Limited`, `Moderate`, or `StrongIndicators`, with the flags stored in JSON.

## Reviewer changes are additive

`AiStance` stays. `EvidenceReview` stores the transition. The product can show both.

## Prompts are files

`claim-extraction-v1` and the other templates live in the AI service. `AiRun` stores the name and version used. A database prompt editor is out of scope.

## URL input is rejected, not half-built

The enum exists. The API returns `INPUT_TYPE_NOT_SUPPORTED`. The form shows the option as unavailable.

## Mock providers are labeled

`MockEvidenceProvider` and `MockLlmClient` are used when keys are absent. Investigations store `evidenceProvider`. The UI must say when results come from fixtures. Tests use the mock LLM so CI does not call a paid API.

## Password hashing

`PasswordHasher<T>` from ASP.NET Core (PBKDF2) avoids an extra crypto dependency and is a known implementation. Argon2id can replace the hasher behind `IPasswordHasher` later without a schema change, because only the hash string is stored.

## 404 for other users’ investigations

A user who guesses an id gets 404, the same as a missing id. Admins and reviewers are the exception and receive the real resource.

## What stayed as specified

Clean layering, JWT and refresh tokens, FluentValidation, structured AI JSON, evidence tabs, six verdicts, audit events, Docker Compose, GitHub Actions, and a verdict calculator covered by explicit tests.
