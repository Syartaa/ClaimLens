# Database

PostgreSQL via EF Core. Primary keys are UUID version 7 (`Guid.CreateVersion7`) so inserts stay ordered without exposing a row count.

Timestamps are `timestamptz` in UTC.

## Entity diagram

```mermaid
erDiagram
  User ||--o{ RefreshToken : has
  User ||--o{ Investigation : owns
  Investigation ||--o{ Claim : contains
  Investigation ||--o{ AuditEvent : records
  Investigation ||--o{ AiRun : traces
  Claim ||--o{ Evidence : assessed_by
  Claim ||--o| AnalysisResult : current_result
  Claim ||--o{ AiRun : traces
  Source ||--o{ Evidence : supports
  Source ||--o| SourceQuality : assessed
  Evidence ||--o{ EvidenceReview : reviewed
  User ||--o{ EvidenceReview : writes

  User {
    uuid Id PK
    string Email UK
    string PasswordHash
    string FirstName
    string LastName
    enum Role
    bool IsActive
    timestamptz CreatedAt
    timestamptz UpdatedAt
  }

  RefreshToken {
    uuid Id PK
    uuid UserId FK
    string TokenHash UK
    timestamptz ExpiresAt
    timestamptz CreatedAt
    timestamptz RevokedAt
    uuid ReplacedByTokenId
  }

  Investigation {
    uuid Id PK
    uuid UserId FK
    string Title
    text OriginalInput
    enum InputType
    enum ProcessingStatus
    enum Verdict
    numeric ConfidenceScore
    text Summary
    string FailureCode
    string EvidenceProvider
    int AttemptCount
    timestamptz CreatedAt
    timestamptz UpdatedAt
    timestamptz CompletedAt
  }

  Claim {
    uuid Id PK
    uuid InvestigationId FK
    int Position
    text ClaimText
    text NormalizedClaim
    enum ClaimType
    numeric ImportanceScore
    uuid CurrentAnalysisResultId FK
  }

  Source {
    uuid Id PK
    string Url UK
    string Domain
    string Title
    string Author
    string Publisher
    timestamptz PublishedAt
    timestamptz RetrievedAt
  }

  SourceQuality {
    uuid Id PK
    uuid SourceId FK UK
    enum Band
    jsonb Factors
    text Explanation
  }

  Evidence {
    uuid Id PK
    uuid ClaimId FK
    uuid SourceId FK
    text EvidenceText
    enum Stance
    numeric RelevanceScore
    numeric ConfidenceScore
    text Explanation
    string PromptVersion
    string ModelUsed
  }

  AnalysisResult {
    uuid Id PK
    uuid ClaimId FK
    enum Verdict
    numeric Confidence
    text Explanation
    string AlgorithmVersion
    timestamptz GeneratedAt
  }

  EvidenceReview {
    uuid Id PK
    uuid EvidenceId FK
    uuid ReviewerId FK
    enum PreviousStance
    enum NewStance
    text Note
    timestamptz CreatedAt
  }

  AiRun {
    uuid Id PK
    uuid InvestigationId FK
    uuid ClaimId FK
    enum Purpose
    string PromptName
    string PromptVersion
    string ModelUsed
    enum Status
    string ErrorCategory
    int LatencyMs
    timestamptz CreatedAt
  }

  AuditEvent {
    uuid Id PK
    uuid InvestigationId FK
    uuid ActorUserId FK
    string EventType
    text Message
    jsonb Metadata
    timestamptz CreatedAt
  }
```

## Relationship rules

| From | To | Delete behavior |
| --- | --- | --- |
| User | Investigation | Restrict. Deactivate users instead of cascading their history away. |
| Investigation | Claim, AuditEvent, AiRun | Cascade. An investigation delete removes its analysis. |
| Claim | Evidence, AnalysisResult | Cascade. |
| Source | Evidence | Restrict. Sources are shared. Delete evidence first. |
| Evidence | EvidenceReview | Cascade. |
| User | RefreshToken | Cascade. |

`Claim.CurrentAnalysisResultId` is a nullable pointer to the latest calculator output. Re-analysis inserts a new `AnalysisResult` and moves the pointer. Older rows remain for traceability.

`Evidence.Stance` is the value the product shows. Reviewer overrides do not replace it in silence: `EvidenceReview` stores previous and new stance, and the displayed stance is updated only as part of that explicit action. The original AI stance is the first review row’s `PreviousStance`, and the evidence row also keeps `AiStance` so the original classification survives even if reviews are listed separately.

Added column on Evidence, not drawn above to keep the diagram readable:

| Column | Purpose |
| --- | --- |
| `AiStance` | Classification returned by the model. Never updated by a reviewer. |
| `Stance` | Current displayed classification. Starts equal to `AiStance`. |

## Indexes

- `users.email` unique, stored lowercase.
- `refresh_tokens.token_hash` unique.
- `investigations (user_id, created_at desc)` for the history list.
- `investigations (processing_status, created_at)` for the worker claim query.
- `claims (investigation_id, position)`.
- `sources.url` unique.
- `evidence (claim_id, stance)`.
- `audit_events (investigation_id, created_at)`.
- `ai_runs (investigation_id, created_at)`.

## Enums

**Role:** `User`, `Reviewer`, `Admin`.

**InputType:** `Claim`, `ArticleText`, `Url`.

**ProcessingStatus:** `Pending`, `ExtractingClaims`, `GatheringEvidence`, `AnalyzingEvidence`, `GeneratingConclusion`, `Completed`, `Failed`.

**Verdict:** `Supported`, `MostlySupported`, `Mixed`, `MostlyContradicted`, `Contradicted`, `InsufficientEvidence`.

**ClaimType:** `Factual`, `Statistical`, `Scientific`, `Historical`, `Medical`, `Political`, `Other`.

**Stance:** `Supporting`, `Contradicting`, `Neutral`.

**SourceQuality.Band:** `Limited`, `Moderate`, `StrongIndicators`. These names describe observed indicators (author, date, HTTPS, primary report, academic or public institution, corroboration). They are not a truth score and never include a “fake” label.

**AiRun.Purpose:** `ClaimExtraction`, `EvidenceClassification`, `Summary`.

**AiRun.Status:** `Succeeded`, `Failed`.

## Source quality factors

Stored as JSON beside the band, for example:

```json
{
  "https": true,
  "hasAuthor": true,
  "hasPublishedAt": true,
  "academicDomain": false,
  "publicInstitution": true,
  "primaryReport": false
}
```

The band is derived from those flags by a documented function, not by a model.

## Seed data

Local seed creates:

- one demo user with a documented development-only password in the seed project, not in git as a real secret pattern beyond the README note that the password is `ChangeMe!Demo1` and must never be used outside Docker
- two completed investigations (one supported-style, one insufficient)
- claims, shared sources, evidence, analysis results, and audit events

The seed password is acceptable only because it is obviously fake and documented. Production startup does not run the demo seed.

## Changes from the original field list

| Original | Change | Why |
| --- | --- | --- |
| `Investigation.Status` | Split into `ProcessingStatus` and `Verdict` | One column cannot mean both “still running” and “mixed evidence”. |
| `AnalysisResult.ModelUsed` | Moved to `AiRun` | The verdict row is produced by code, so it stores `AlgorithmVersion`. |
| Refresh tokens missing | `RefreshToken` table | Required for rotation and logout. |
| Evidence classification overwritten later | `AiStance` plus `EvidenceReview` | Reviewers must not erase the model output. |
| Source embedded on evidence | Shared `Source` by unique URL | The same article can support multiple claims. |
| Prompt rows in the database | Versioned files plus columns on `AiRun` | A prompt CMS is a product of its own. Traceability still exists. |
