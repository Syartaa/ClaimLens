# API

Base path `/api`. JSON. Authenticated routes expect `Authorization: Bearer <access-token>`.

Errors use one shape:

```json
{
  "code": "VALIDATION_FAILED",
  "message": "The claim text is required.",
  "details": [{ "field": "text", "message": "The claim text is required." }]
}
```

`details` is omitted when there is nothing field-specific.

## Auth

| Method | Path | Auth | Success | Notes |
| --- | --- | --- | --- | --- |
| POST | `/api/auth/register` | No | 201 | Body: email, password, firstName, lastName. Role is always `User`. |
| POST | `/api/auth/login` | No | 200 | Returns access token, refresh token, user summary. |
| POST | `/api/auth/refresh` | No | 200 | Rotates the refresh token. Reuse of a rotated token revokes the family. |
| POST | `/api/auth/logout` | Yes | 204 | Revokes the presented refresh token. |
| GET | `/api/auth/me` | Yes | 200 | Current user profile. |

## Investigations

| Method | Path | Auth | Success |
| --- | --- | --- | --- |
| GET | `/api/investigations` | User | 200 list |
| POST | `/api/investigations` | User | 202 |
| GET | `/api/investigations/{id}` | Owner, Reviewer, Admin | 200 |
| DELETE | `/api/investigations/{id}` | Owner or Admin | 204 |
| POST | `/api/investigations/{id}/reanalyze` | Owner or Admin | 202 |
| GET | `/api/investigations/{id}/claims` | Owner, Reviewer, Admin | 200 |
| GET | `/api/investigations/{id}/timeline` | Owner, Reviewer, Admin | 200 |

`POST /api/investigations` body:

```json
{
  "inputType": "Claim",
  "title": "Coffee and dehydration",
  "text": "Drinking coffee causes dehydration."
}
```

`inputType` of `Url` returns 400 `INPUT_TYPE_NOT_SUPPORTED` until URL fetching exists.

List query: `page` (default 1), `pageSize` (default 20, max 50), `q`, `verdict`, `processingStatus`, `inputType`, `from`, `to`, `sort` (`createdAt` or `updatedAt`, prefix `-` for descending).

List item fields: id, title, inputType, processingStatus, verdict, confidenceScore, createdAt, completedAt.

Detail adds originalInput, summary, failureCode, evidenceProvider, claims, and per-claim evidence grouped by stance. Evidence includes source title, domain, url, publishedAt, excerpt, explanation, relevance, confidence, aiStance, stance, and quality band.

## Evidence

| Method | Path | Auth | Success |
| --- | --- | --- | --- |
| GET | `/api/claims/{id}/evidence` | Owner of parent investigation, Reviewer, Admin | 200 |

## Reviews (after MVP)

| Method | Path | Auth | Success |
| --- | --- | --- | --- |
| POST | `/api/reviews/evidence/{id}` | Reviewer, Admin | 201 |
| POST | `/api/reviews/investigations/{id}/approve` | Reviewer, Admin | 200 |

Override body: `{ "stance": "Neutral", "note": "Excerpt is background, not a result." }`

## Admin (after MVP)

| Method | Path | Auth | Success |
| --- | --- | --- | --- |
| GET | `/api/admin/dashboard` | Admin | 200 |
| GET | `/api/admin/users` | Admin | 200 |
| GET | `/api/admin/investigations` | Admin | 200 |
| GET | `/api/admin/ai-runs` | Admin | 200 failed and recent runs |

## Health

| Method | Path | Auth | Success |
| --- | --- | --- | --- |
| GET | `/api/health` | No | 200 |

## Pagination

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0
}
```

## Status codes

| Code | When |
| --- | --- |
| 400 | Validation, unsupported input, bad JSON from a client |
| 401 | Missing or expired access token |
| 403 | Authenticated but not allowed |
| 404 | Missing entity, including another user’s investigation (no existence leak) |
| 409 | Reanalyze while a run is already in progress |
| 429 | Rate limit |
| 500 | Unexpected failure with a generic code `INTERNAL_ERROR` |

## Internal AI contract

Not exposed through the public API. Called by the worker with header `X-Internal-Key`.

Invalid model JSON is an AI failure. The API stores `AiRun.Status = Failed` and does not coerce unknown fields into a verdict.

## Validation examples

| Case | Code |
| --- | --- |
| Empty text | `VALIDATION_FAILED` |
| Claim longer than the claim limit | `VALIDATION_FAILED` |
| Article longer than the article limit | `VALIDATION_FAILED` |
| Unknown input type | `VALIDATION_FAILED` |
| `pageSize` above 50 | `VALIDATION_FAILED` |
| Unknown id format | `VALIDATION_FAILED` |
