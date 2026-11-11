# Frontend

React Router. Authenticated pages sit under a workspace shell (sidebar, account menu). Public pages use a quieter marketing layout.

## Routes

| Path | Page | Access |
| --- | --- | --- |
| `/` | Landing | Public |
| `/login` | Login | Public, redirect if signed in |
| `/register` | Register | Public, redirect if signed in |
| `/app` | Dashboard | User |
| `/app/investigations/new` | New investigation | User |
| `/app/investigations` | History | User |
| `/app/investigations/:id` | Investigation detail | Owner; reviewer and admin later |
| `/app/profile` | Profile | User |
| `/app/review` | Reviewer queue | Reviewer, after MVP |
| `/app/admin` | Admin dashboard | Admin, after MVP |

Unknown paths render a not-found page. A failed auth check returns to `/login` and keeps the attempted path for the next successful login.

## New investigation

Question: “What would you like to investigate?”

Toggle: Claim or Article text. URL is visible as disabled with the label “Coming later” so the product boundary is honest.

Submit calls `POST /api/investigations` and navigates to `/app/investigations/:id`.

## Investigation detail

This is the main screen.

1. Title and input type.
2. Verdict badge, or a processing badge while the run is unfinished.
3. System confidence, labeled “System confidence”, with a one-line note that it is not a probability the claim is true.
4. Summary.
5. Claims. One claim is expanded by default. Extra claims are separate sections.
6. Evidence sections: Supporting, Contradicting, Context. Each card shows publisher or domain, title, date, excerpt, explanation, relevance, classification confidence, quality band, and the source link.
7. Timeline built from audit events: claim extracted, evidence gathered, evidence analyzed, conclusion calculated.

While `processingStatus` is not `Completed` or `Failed`, the page polls. The status line uses the enum labels (“Gathering evidence”), not a spinner with no text.

Failed runs show the safe message and a retry action that calls reanalyze.

If `evidenceProvider` is `mock`, a banner states that sources are development fixtures.

Empty evidence and `InsufficientEvidence` use the same honest empty state: no sources were strong enough to classify the claim.

## Dashboard

Counts: total, completed, in progress, failed, claims analyzed.

Distribution of verdicts among completed investigations.

Recent investigations.

Search and filters for verdict, processing status, input type, and date. Filters live in the query string so a view can be shared.

## History

Same filters as the dashboard list, plus sort and pagination. Row actions: open, delete (confirm), re-run.

## States

Each data page implements loading skeletons, an empty state with a next action, and an error state that shows the API `message` when the code is safe, otherwise “Something went wrong. Try again.”

Forms expose errors on the field, use visible labels, and can be completed from the keyboard.

## Client structure

```text
apps/web/src/
  main.tsx
  app/
    router.tsx
    providers.tsx
  features/
    auth/
    dashboard/
    investigations/
    profile/
  components/
    ui/             shadcn primitives
    layout/
  lib/
    api-client.ts
    query-keys.ts
```

Feature folders own their API hooks and view components. Shared buttons, inputs, and badges stay in `components/ui`.
