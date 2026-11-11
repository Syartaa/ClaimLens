# Requirements

## Problem

People meet claims in articles, posts, and conversation and have no compact way to see the claim, the sources, and the disagreement. A single true/false label hides that work and overstates what a model knows.

## Product

ClaimLens turns an input into an investigation:

1. Extract one or more factual claims.
2. Identify entities and topics worth searching.
3. Retrieve evidence through a provider interface.
4. Classify each evidence item as supporting, contradicting, or neutral.
5. Explain the classification in plain language.
6. Show sources.
7. Calculate a verdict and a system confidence score in backend code.
8. Say **Insufficient Evidence** when the record is too thin.

A conclusion is never shown without the claims, evidence, sources, and calculation that produced it.

## Users and roles

| Role | Can do |
| --- | --- |
| User | Register, sign in, create and read own investigations, delete own investigations, re-run own investigations |
| Reviewer | Read investigations, confirm or override evidence classification, flag a source, add notes, approve an investigation. Original AI values stay stored. |
| Admin | Everything a reviewer can do, plus user and system monitoring |

Reviewer and admin **screens** are after MVP. The schema and authorization policies are designed now so those screens do not require a rewrite.

## Inputs

| Input | MVP | Notes |
| --- | --- | --- |
| Written claim | Yes | The submitted text is the claim. Extraction still normalizes it. |
| Article text | Yes | The AI service returns a list of factual claims as JSON. |
| URL | No | Fetching URLs adds SSRF risk and parsing work. The enum value exists so the API can reject it clearly until that phase. |

## Verdicts

`Supported`, `MostlySupported`, `Mixed`, `MostlyContradicted`, `Contradicted`, `InsufficientEvidence`.

These describe the **evidence on record**, not objective truth.

## Processing states

`Pending`, `ExtractingClaims`, `GatheringEvidence`, `AnalyzingEvidence`, `GeneratingConclusion`, `Completed`, `Failed`.

Processing state and verdict are different fields. See [ADR 0002](adr/0002-processing-status-vs-verdict.md).

## Principles

- Evidence and sources outrank the generated summary.
- The LLM extracts, structures, summarizes, and classifies. It does not emit the final verdict.
- Every AI payload is schema-validated before it is stored.
- Uncertainty is a first-class result.
- Mock evidence is stored with provider name `mock` and is never presented as a live web search.

## Non-goals for the first shippable slice

URL ingestion, reviewer UI, admin UI, SignalR, Redis, Hangfire, production hosting, and a public demo. See [roadmap](roadmap.md).
