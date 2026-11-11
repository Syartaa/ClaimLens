# ADR 0003 — PostgreSQL backs the analysis queue

## Status

Accepted for MVP. Revisit if more than one kind of job appears or operators need a job dashboard.

## Context

Analysis calls are too slow for the HTTP request that creates an investigation. Hangfire and Redis would add services before the pipeline exists.

## Decision

`POST /api/investigations` inserts a row with `ProcessingStatus = Pending` and returns 202.

A hosted worker in the API process:

1. Opens a transaction.
2. Selects the oldest `Pending` or stale in-progress row with `FOR UPDATE SKIP LOCKED`.
3. Sets the next processing status and commits.
4. Does the stage work.
5. On process crash, a row left in a non-terminal status past a timeout returns to `Pending` or moves to `Failed` with `WORKER_INTERRUPTED`, according to how many attempts are stored.

Attempt count lives on the investigation (`AttemptCount`, max 3) so a poison input cannot loop forever.

## Consequences

Local Docker Compose does not include Redis. SignalR is still a later phase; clients poll. A second API instance can run the same worker safely because of row locks.
