# ADR 0001 — The verdict is deterministic

## Status

Accepted for Phase 1. Implement in Phase 8.

## Decision

`ClaimVerdictCalculator` in the domain project maps stored evidence to a verdict and a system confidence score. The LLM is not asked for either number.

Every cutoff below is an **engineering heuristic**. The numbers are not scientifically validated, and they are not the probability that a claim is true. System confidence means only how strongly this evidence set leans under the current rules.

All cutoffs live in one place: `VerdictThresholds` in the domain project (`apps/api/src/ClaimLens.Domain/Verdict/VerdictThresholds.cs`). The calculator reads those named constants and does not embed new literals. Phase 8 locks the behavior with unit tests. Changing a constant requires a test change in the same commit.

## Algorithm

An evidence item is **usable** when relevance is at least `MinimumRelevance` (0.40) and classification confidence is at least `MinimumClassificationConfidence` (0.50).

Quality multipliers: `Limited` 0.70, `Moderate` 1.00, `StrongIndicators` 1.15. Weight of one item is `relevance * confidence * multiplier`, capped at `MaximumEvidenceWeight` (1.15).

Items that share a registrable domain count as one independent source. Domain is taken from the source URL.

If there are fewer than two independent usable sources, the claim is `InsufficientEvidence`, including the case of a single weak source. A single source can still be usable for display. It cannot carry a stronger verdict alone.

Otherwise:

- `support` = sum of weights for supporting items
- `contradict` = sum of weights for contradicting items
- Neutral items do not add to either sum. They remain visible.
- `total = support + contradict`
- `balance = (support - contradict) / total` in the range -1 to 1

| Condition | Constant | Verdict |
| --- | --- | --- |
| `total` below 1.20 | `MinimumTotalWeight` | `InsufficientEvidence` |
| `balance` >= 0.60 and contradict is small relative to support | `SupportedBalance` | `Supported` |
| `balance` >= 0.25 | `MostlySupportedBalance` | `MostlySupported` |
| `balance` <= -0.60 | `ContradictedBalance` | `Contradicted` |
| `balance` <= -0.25 | `MostlyContradictedBalance` | `MostlyContradicted` |
| otherwise |  | `Mixed` |

“Contradict is small” means `contradict / total <= MaxContradictionShareForSupported` (0.15). If support is high but a real contradicting source remains, the verdict stays at `MostlySupported` or `Mixed` rather than `Supported`.

System confidence is not the chance the claim is true, and it is not a scientifically validated probability. It starts from the absolute balance, then increases slightly with more independent domains, and is clamped between `MinimumReportedConfidence` (0.35) and `MaximumReportedConfidence` (0.95). `InsufficientEvidence` uses at most `InsufficientEvidenceConfidenceCap` (0.40) so the UI does not look certain about a thin record.

Investigation verdict: weight each claim by `ImportanceScore`. If the two highest-importance claims fall on opposite sides of neutral (one of supported/mostly supported, the other contradicted/mostly contradicted), the investigation is `Mixed`. Otherwise the investigation takes the verdict of the highest-importance claim, and confidence is the importance-weighted mean.

Thresholds are named constants. Tests lock the table in the roadmap. Changing a threshold requires a test change in the same commit.

## Consequences

Explanations can cite counts, domains, and the band that was applied. The project does not claim a scientific truth probability.
