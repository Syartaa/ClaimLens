import { HubConnectionBuilder, HubConnectionState } from "@microsoft/signalr";
import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ApiError, api, baseUrl } from "../auth/api";
import { canReview, getAccessToken, sessionUserId } from "../auth/session";
import type { Investigation } from "../investigations/types";
import { dangerButton, fieldClass, Pill, primaryButton, secondaryButton } from "../ui";

export function InvestigationDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [investigation, setInvestigation] = useState<Investigation | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [live, setLive] = useState(false);

  useEffect(() => {
    if (!id) {
      return;
    }

    let cancelled = false;
    let timer = 0;
    const running = (status: string) =>
      status === "Pending" ||
      status === "ExtractingClaims" ||
      status === "GatheringEvidence" ||
      status === "AnalyzingEvidence" ||
      status === "GeneratingConclusion";

    const load = () => {
      api<Investigation>(`/api/investigations/${id}`, {}, true)
        .then((body) => {
          if (cancelled) {
            return;
          }
          setInvestigation(body);
          if (running(body.processingStatus) && connection.state !== HubConnectionState.Connected) {
            timer = window.setTimeout(load, 2000);
          }
        })
        .catch((caught: unknown) => {
          if (!cancelled) {
            setError(caught instanceof ApiError ? caught.message : "Investigation could not be loaded.");
          }
        });
    };

    const connection = new HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/investigations`, { accessTokenFactory: () => getAccessToken() ?? "" })
      .withAutomaticReconnect()
      .build();
    connection.on("statusChanged", () => {
      window.clearTimeout(timer);
      load();
    });
    connection.onreconnected(() => {
      setLive(true);
      void connection.invoke("Join", id).then(load);
    });
    connection.onclose(() => {
      setLive(false);
      load();
    });

    void connection
      .start()
      .then(() => connection.invoke("Join", id))
      .then(() => {
        if (!cancelled) {
          setLive(true);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setLive(false);
        }
      });

    load();
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
      setLive(false);
      void connection.stop();
    };
  }, [id]);

  async function cancel() {
    if (!id || !window.confirm("Cancel this investigation?")) {
      return;
    }

    try {
      const stopped = await api<Investigation>(`/api/investigations/${id}/cancel`, { method: "POST" }, true);
      setInvestigation(stopped);
      setActionError(null);
    } catch (caught) {
      setActionError(caught instanceof ApiError ? caught.message : "Something went wrong. Try again.");
    }
  }

  async function rerun() {
    if (!id) {
      return;
    }

    try {
      const restarted = await api<Investigation>(`/api/investigations/${id}/reanalyze`, { method: "POST" }, true);
      setInvestigation(restarted);
      setActionError(null);
    } catch (caught) {
      setActionError(caught instanceof ApiError ? caught.message : "Something went wrong. Try again.");
    }
  }

  async function remove() {
    if (!id || !window.confirm("Delete this investigation?")) {
      return;
    }

    try {
      await api(`/api/investigations/${id}`, { method: "DELETE" }, true);
      navigate("/app/investigations");
    } catch (caught) {
      setActionError(caught instanceof ApiError ? caught.message : "The investigation could not be deleted.");
    }
  }

  if (error) {
    return (
      <p className="border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-900" role="alert">
        {error}
      </p>
    );
  }

  if (!investigation) {
    return <p role="status">Loading investigation</p>;
  }

  const running = isRunning(investigation.processingStatus);
  const owner = sessionUserId() === investigation.ownerId;

  return (
    <article>
      <div className="flex flex-wrap items-center gap-2">
        <Pill value={investigation.inputType} />
        <Pill value={investigation.processingStatus} />
        {investigation.processingStatus === "Completed" && investigation.verdict ? <Pill value={investigation.verdict} /> : null}
        <span className="font-sans text-sm text-stone-500">{live ? "Live updates" : "Checking every few seconds"}</span>
      </div>
      <h1 className="mt-3 text-3xl font-semibold tracking-tight">{investigation.title}</h1>
      {actionError ? (
        <p className="mt-4 rounded-md border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-900" role="alert">
          {actionError}
        </p>
      ) : null}
      <ProgressSteps status={investigation.processingStatus} />
      <section className={`mt-6 max-w-2xl rounded-lg border px-4 py-4 ${resultCardClass(investigation)}`}>
        <h2 className="font-sans text-sm font-medium">What this result means</h2>
        <p className="mt-2">{statusExplanation(investigation.processingStatus, investigation.failureCode)}</p>
        {investigation.processingStatus === "Completed" && investigation.confidenceScore != null ? (
          <p className="mt-3">
            System confidence {investigation.confidenceScore.toFixed(2)}. This is not the probability the claim is true.
          </p>
        ) : null}
        {investigation.summary ? <p className="mt-3">{investigation.summary}</p> : null}
        {investigation.verdict === "InsufficientEvidence" ? (
          <p className="mt-3 text-sm">No sources were strong enough to classify this claim.</p>
        ) : null}
        {running ? <p className="mt-3 text-sm">Stay on this page. It updates by itself.</p> : null}
      </section>
      {investigation.approvedAt ? (
        <p className="mt-4 max-w-2xl rounded-lg border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm text-emerald-950" role="status">
          Approved {new Date(investigation.approvedAt).toLocaleString()}. Approval does not change the calculated verdict.
        </p>
      ) : null}
      {investigation.evidenceProvider === "mock" ? (
        <p className="mt-4 max-w-2xl rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-950" role="status">
          These sources are development fixtures, not live web search.
        </p>
      ) : null}
      <div className="mt-4 flex flex-wrap gap-2">
        <Link className={secondaryButton} to="/app/investigations">
          Back to history
        </Link>
        <a className={secondaryButton} href="#sources">
          See sources
        </a>
        {owner && running ? (
          <button className={dangerButton} type="button" onClick={() => void cancel()}>
            Cancel
          </button>
        ) : null}
        {owner && (investigation.processingStatus === "Failed" || investigation.processingStatus === "Completed") ? (
          <button className={secondaryButton} type="button" onClick={() => void rerun()}>
            Re-run
          </button>
        ) : null}
        {owner ? (
          <button className={dangerButton} type="button" onClick={() => void remove()}>
            Delete
          </button>
        ) : null}
        {canReview() && investigation.processingStatus === "Completed" && !investigation.approvedAt ? (
          <a className={primaryButton} href="#approve">
            Approve
          </a>
        ) : null}
      </div>
      {(investigation.claims ?? []).length > 0 ? (
        <section className="mt-8" id="sources">
          <h2 className="text-lg font-medium">Extracted claims</h2>
          <ol className="mt-3 list-decimal space-y-3 pl-5">
            {(investigation.claims ?? []).map((claim) => (
              <li key={claim.id}>
                <p>{claim.claimText}</p>
                <p className="text-sm text-stone-500">
                  {claim.claimType} · importance {claim.importanceScore.toFixed(2)}
                </p>
                <EvidenceList
                  investigationId={investigation.id}
                  items={claim.evidence ?? []}
                  canReview={canReview() && investigation.processingStatus === "Completed"}
                  onUpdated={(updated) => {
                    setInvestigation(updated);
                    setActionError(null);
                  }}
                  onError={setActionError}
                />
              </li>
            ))}
          </ol>
        </section>
      ) : null}
      <h2 className="mt-8 text-lg font-medium">Original input</h2>
      <p className="mt-2 whitespace-pre-wrap rounded-lg border border-stone-200 bg-white px-4 py-3">{investigation.originalInput}</p>
      {(investigation.timeline ?? []).length > 0 ? (
        <section className="mt-8">
          <h2 className="text-lg font-medium">Timeline</h2>
          <ol className="mt-3 space-y-3 border-l border-stone-300 pl-4">
            {investigation.timeline.map((event) => (
              <li key={`${event.type}-${event.at}`}>
                <p className="text-sm font-medium">{event.message}</p>
                <p className="text-sm text-stone-500">{new Date(event.at).toLocaleString()}</p>
              </li>
            ))}
          </ol>
        </section>
      ) : null}
      {canReview() && investigation.processingStatus === "Completed" && !investigation.approvedAt ? (
        <ApproveInvestigation
          investigationId={investigation.id}
          onUpdated={(updated) => {
            setInvestigation(updated);
            setActionError(null);
          }}
          onError={setActionError}
        />
      ) : null}
    </article>
  );
}

function EvidenceList({
  investigationId,
  items,
  canReview,
  onUpdated,
  onError,
}: {
  investigationId: string;
  items: NonNullable<Investigation["claims"][number]["evidence"]>;
  canReview: boolean;
  onUpdated: (investigation: Investigation) => void;
  onError: (message: string) => void;
}) {
  if (items.length === 0) {
    return <p className="mt-2 text-sm text-stone-500">No sources retrieved for this claim yet.</p>;
  }

  const groups = [
    ["Supporting", "border-emerald-300 bg-emerald-50/40", items.filter((item) => item.stance === "Supporting")],
    ["Contradicting", "border-rose-300 bg-rose-50/40", items.filter((item) => item.stance === "Contradicting")],
    ["Context", "border-stone-200 bg-white", items.filter((item) => item.stance === "Neutral" || item.stance == null)],
  ] as const;

  return (
    <div className="mt-3 space-y-4">
      {groups.map(([label, tone, group]) =>
        group.length === 0 ? null : (
          <section key={label}>
            <h3 className="font-sans text-sm font-medium">{label}</h3>
            <ul className="mt-2 space-y-3">
              {group.map((item) => (
                <li key={item.id} className={`rounded-lg border px-4 py-3 ${tone}`}>
                  <p className="font-medium">{item.sourceTitle}</p>
                  <p className="text-sm text-stone-500">
                    {item.publisher || item.domain}
                    {item.publishedAt ? ` · ${new Date(item.publishedAt).toLocaleDateString()}` : ""}
                    {item.qualityBand ? ` · ${item.qualityBand}` : ""}
                  </p>
                  <p className="mt-2 text-sm">AI stance {item.aiStance ?? "not classified"}</p>
                  <p className="text-sm">Reviewed stance {item.stance ?? "not set"}</p>
                  {item.reviewNote ? <p className="mt-1 text-sm text-stone-600">Review note: {item.reviewNote}</p> : null}
                  {item.sourceFlagged ? (
                    <p className="mt-1 text-sm text-stone-800">Source flagged{item.flagNote ? `: ${item.flagNote}` : ""}</p>
                  ) : null}
                  <p className="mt-2 text-sm">{item.excerpt}</p>
                  {item.explanation ? <p className="mt-2 text-sm text-stone-600">{item.explanation}</p> : null}
                  {item.relevanceScore != null && item.confidenceScore != null ? (
                    <p className="mt-2 text-sm text-stone-500">
                      Relevance {item.relevanceScore.toFixed(2)} · classification confidence {item.confidenceScore.toFixed(2)}. These scores are not the probability the claim is true.
                    </p>
                  ) : null}
                  <a className="mt-2 inline-block text-sm" href={item.url}>
                    {item.url}
                  </a>
                  {canReview ? (
                    <EvidenceReviewForm
                      investigationId={investigationId}
                      evidenceId={item.id}
                      sourceId={item.sourceId}
                      currentStance={item.stance}
                      onUpdated={onUpdated}
                      onError={onError}
                    />
                  ) : null}
                </li>
              ))}
            </ul>
          </section>
        ),
      )}
    </div>
  );
}

function EvidenceReviewForm({
  investigationId,
  evidenceId,
  sourceId,
  currentStance,
  onUpdated,
  onError,
}: {
  investigationId: string;
  evidenceId: string;
  sourceId: string;
  currentStance: string | null;
  onUpdated: (investigation: Investigation) => void;
  onError: (message: string) => void;
}) {
  const [stance, setStance] = useState(currentStance ?? "Neutral");
  const [note, setNote] = useState("");
  const [pending, setPending] = useState(false);

  async function submitStance() {
    setPending(true);
    try {
      const updated = await api<Investigation>(
        `/api/reviews/evidence/${evidenceId}`,
        { method: "POST", body: JSON.stringify({ stance, note }) },
        true,
      );
      setNote("");
      onUpdated(updated);
    } catch (caught) {
      onError(caught instanceof ApiError ? caught.message : "The stance could not be saved.");
    } finally {
      setPending(false);
    }
  }

  async function flagSource() {
    setPending(true);
    try {
      const updated = await api<Investigation>(
        `/api/reviews/sources/${sourceId}/flag`,
        { method: "POST", body: JSON.stringify({ investigationId, note }) },
        true,
      );
      setNote("");
      onUpdated(updated);
    } catch (caught) {
      onError(caught instanceof ApiError ? caught.message : "The source could not be flagged.");
    } finally {
      setPending(false);
    }
  }

  const confirming = stance === currentStance;

  return (
    <form
      className="mt-3 flex flex-col gap-2 border-t border-stone-200 pt-3"
      onSubmit={(event) => {
        event.preventDefault();
        void submitStance();
      }}
    >
      <p className="font-sans text-sm font-medium text-stone-800">Your review</p>
      <p className="text-sm text-stone-600">A reviewed stance does not replace the calculated verdict.</p>
      <label className="flex flex-col gap-1 text-sm text-stone-700">
        Stance
        <select
          className={fieldClass}
          value={stance}
          onChange={(event) => setStance(event.target.value)}
        >
          <option value="Supporting">Supporting</option>
          <option value="Contradicting">Contradicting</option>
          <option value="Neutral">Neutral</option>
        </select>
      </label>
      <label className="flex flex-col gap-1 text-sm text-stone-700">
        Note
        <textarea
          className={fieldClass}
          value={note}
          onChange={(event) => setNote(event.target.value)}
          required
          maxLength={1000}
          placeholder="Say why you agree or disagree."
        />
      </label>
      <div className="flex flex-wrap gap-2">
        <button className={confirming ? primaryButton : "inline-flex items-center justify-center rounded-md bg-amber-700 px-4 py-2 text-sm font-medium font-sans text-white hover:bg-amber-800 disabled:opacity-60"} type="submit" disabled={pending}>
          {confirming ? "Confirm stance" : "Override stance"}
        </button>
        <button className={dangerButton} type="button" disabled={pending} onClick={() => void flagSource()}>
          Flag source
        </button>
      </div>
    </form>
  );
}

function ApproveInvestigation({
  investigationId,
  onUpdated,
  onError,
}: {
  investigationId: string;
  onUpdated: (investigation: Investigation) => void;
  onError: (message: string) => void;
}) {
  const [note, setNote] = useState("");
  const [pending, setPending] = useState(false);

  async function approve() {
    setPending(true);
    try {
      const updated = await api<Investigation>(
        `/api/reviews/investigations/${investigationId}/approve`,
        { method: "POST", body: JSON.stringify({ note }) },
        true,
      );
      onUpdated(updated);
    } catch (caught) {
      onError(caught instanceof ApiError ? caught.message : "The investigation could not be approved.");
    } finally {
      setPending(false);
    }
  }

  return (
    <form
      id="approve"
      className="mt-8 max-w-xl rounded-lg border border-teal-300 bg-teal-50 px-4 py-4"
      onSubmit={(event) => {
        event.preventDefault();
        void approve();
      }}
    >
      <h2 className="text-lg font-medium">Approve investigation</h2>
      <p className="mt-2 text-sm text-teal-950">Use this after you have checked the sources. Approval does not change the calculated verdict.</p>
      <label className="mt-3 flex flex-col gap-1 text-sm text-stone-700">
        Note, optional
        <textarea className={fieldClass} value={note} onChange={(event) => setNote(event.target.value)} maxLength={1000} />
      </label>
      <button className={`${primaryButton} mt-3`} type="submit" disabled={pending}>
        {pending ? "Saving" : "Approve"}
      </button>
    </form>
  );
}

function isRunning(status: string): boolean {
  return status !== "Completed" && status !== "Failed";
}

function resultCardClass(investigation: Investigation): string {
  if (investigation.processingStatus === "Failed") {
    return "border-rose-300 bg-rose-50 text-rose-950";
  }
  if (investigation.processingStatus !== "Completed") {
    return "border-sky-300 bg-sky-50 text-sky-950";
  }
  if (investigation.verdict === "Supported" || investigation.verdict === "MostlySupported") {
    return "border-emerald-300 bg-emerald-50 text-emerald-950";
  }
  if (investigation.verdict === "Contradicted" || investigation.verdict === "MostlyContradicted") {
    return "border-rose-300 bg-rose-50 text-rose-950";
  }
  if (investigation.verdict === "Mixed") {
    return "border-amber-300 bg-amber-50 text-amber-950";
  }
  return "border-stone-300 bg-white text-stone-800";
}

const progressSteps = ["Extract claims", "Collect sources", "Classify", "Verdict"];

function ProgressSteps({ status }: { status: string }) {
  const current = status === "Pending" || status === "ExtractingClaims"
    ? 0
    : status === "GatheringEvidence"
      ? 1
      : status === "AnalyzingEvidence"
        ? 2
        : status === "GeneratingConclusion" || status === "Completed"
          ? 3
          : -1;

  return (
    <ol className="mt-6 grid gap-2 sm:grid-cols-4">
      {progressSteps.map((label, index) => {
        const done = status === "Completed" || (current > index && status !== "Failed");
        const active = status !== "Failed" && status !== "Completed" && current === index;
        const tone = done
          ? "border-emerald-300 bg-emerald-50 text-emerald-950"
          : active
            ? "border-teal-700 bg-teal-800 text-white"
            : "border-stone-200 bg-white text-stone-500";
        return (
          <li key={label} className={`rounded-lg border px-3 py-2 font-sans text-sm ${tone}`}>
            <span className="text-xs font-medium">{index + 1}</span>
            <span className="mt-1 block">{label}</span>
          </li>
        );
      })}
    </ol>
  );
}

function statusExplanation(status: string, failureCode: string | null): string {
  if (status === "Failed") {
    if (failureCode === "AI_RESPONSE_INVALID") {
      return "The model response was not valid, so no claims were saved.";
    }
    if (failureCode === "EVIDENCE_RESPONSE_INVALID" || failureCode === "EVIDENCE_REQUEST_FAILED") {
      return "Evidence collection failed, so no sources were saved.";
    }
    if (failureCode === "CANCELLED") {
      return "You cancelled this run. Re-run starts a fresh attempt.";
    }
    if (failureCode === "WORKER_INTERRUPTED") {
      return "The worker stopped mid-run three times, so the investigation was not retried again.";
    }
    return "The investigation failed. ClaimLens did not invent missing results.";
  }
  if (status === "GatheringEvidence") {
    return "Claims are saved. ClaimLens is retrieving sources. Classification has not started.";
  }
  if (status === "AnalyzingEvidence") {
    return "Sources are saved. ClaimLens is classifying each excerpt. The verdict has not been calculated.";
  }
  if (status === "GeneratingConclusion") {
    return "Stances are saved. The verdict is being calculated from those scores. The model does not choose it.";
  }
  if (status === "Completed") {
    return "The verdict comes from the evidence rules, not from the model.";
  }
  return "ClaimLens is reading the text and extracting factual claims.";
}
