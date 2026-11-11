# Roadmap

Phases match the specification. Scope inside each phase is tightened so the repository stays reviewable.

## MVP

Phases 1 through 9, with these boundaries:

- Claim and article text only.
- One in-process worker using PostgreSQL as the queue (Phase 10’s idea, a smaller mechanism). The UI polls.
- `MockEvidenceProvider` when no search API is configured. Records store `evidenceProvider = mock`.
- One OpenAI-compatible LLM path, plus a mock LLM used in tests and local runs without a key.
- User role end to end. Reviewer and Admin exist on the user record and in authorization policies, without their dashboards.
- Unit tests for the verdict calculator and validation of AI payloads. One API integration test for auth and investigation ownership.

## After MVP

| Item | Why it waits |
| --- | --- |
| URL fetch | SSRF controls and article extraction are a separate project slice. |
| Live web search provider | Needs an API key, quotas, and source-normalization rules. The port exists from the evidence phase. |
| SignalR | Polling is enough until the pipeline is trustworthy. |
| Redis | One API instance does not need it. |
| Hangfire | The Postgres worker covers restart safety. Hangfire is optional if operations need a dashboard. |
| Reviewer UI | Schema and audit events are ready. The workflow is a product phase. |
| Admin UI | Same. |
| Playwright journey | Add once register, login, and a mock investigation complete in Docker. |
| Production deploy and a public demo | After CI is green on the MVP. |

## Phase plan

### Phase 1 — Planning (this phase)

Documents, architecture diagrams, and the monorepo folders. No application code.

### Phase 2 — Setup

Scaffold `apps/web`, `apps/api`, and `apps/ai-service`. Docker Compose for web, api, ai-service, and PostgreSQL. Health endpoints. `.env.example` already lists the variables. GitHub Actions builds all three and runs the empty test projects.

### Phase 3 — Authentication

Register, login, refresh, logout, me. Hashed passwords. Protected routes. Integration tests for duplicate email, bad password, and refresh reuse.

### Phase 4 — Investigation CRUD

Create, list, get, delete, filter, paginate. Processing stays `Pending`. No AI calls.

### Phase 5 — Claim extraction

Worker claims `Pending` rows. AI client with schema validation. Persist claims and `AiRun`. Invalid JSON fails the investigation cleanly.

### Phase 6 — Evidence

`IEvidenceProvider`, mock implementation, `Source`, `SourceQuality`, `Evidence` rows.

### Phase 7 — Classification

Classify each pair. Validate stance and score ranges. Persist `AiStance` and `Stance`.

### Phase 8 — Verdict engine

`ClaimVerdictCalculator` in the domain project. Table-driven tests:

| Fixture | Expected |
| --- | --- |
| Several strong supporting sources, no serious contradiction | `Supported` |
| Several strong contradicting sources only | `Contradicted` |
| Similar support and contradiction from independent sources | `Mixed` |
| No usable evidence | `InsufficientEvidence` |
| One weak source | `InsufficientEvidence` |
| More independent strong sources | higher system confidence than a single strong source |

Then generate the summary from the calculator output.

### Phase 9 — Results UI

Detail page, evidence cards, timeline, dashboard, history, empty and error states.

### Phase 10 — Durable worker polish

Retries, cancellation, and a documented recovery path if the process dies mid-run. This phase hardens the worker introduced in Phase 5 rather than introducing a new queue product.

### Phase 11 — SignalR

Push processing status. Polling remains the fallback.

### Phase 12 — Reviewer

Confirm, override, flag source, notes, approve. Audit events. UI shows AI stance and reviewed stance.

### Phase 13 — Admin

Users, failures, AI run errors, provider usage.

### Phase 14 — Hardening

Authorization matrix tests, rate limits, security headers, Playwright path: register, login, create, see a completed mock investigation.

### Phase 15 — Deployment

Production compose or host-specific config, public demo, CI badge on the README.

## How to move to the next phase

Phase 2 should scaffold the three apps, compose file, and CI only. It should not implement login or investigations.
