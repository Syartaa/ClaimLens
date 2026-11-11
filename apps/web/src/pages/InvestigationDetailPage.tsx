import { HubConnectionBuilder, HubConnectionState } from "@microsoft/signalr";
import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ApiError, api, baseUrl } from "../auth/api";
import { canReview, getAccessToken, sessionUserId } from "../auth/session";
import type { Investigation } from "../investigations/types";

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

  return (
    <article>
      <p className="text-sm text-stone-500">{investigation.inputType}</p>
      <h1 className="mt-2 text-3xl font-semibold tracking-tight">{investigation.title}</h1>
      <p className="mt-4 text-sm text-stone-500">{live ? "Live updates" : "Polling for updates"}</p>
      {actionError ? (
        <p className="mt-4 border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-900" role="alert">
          {actionError}
        </p>
      ) : null}
      {investigation.approvedAt ? (
        <p className="mt-4 border border-stone-300 bg-white px-4 py-3 text-sm" role="status">
          Approved {new Date(investigation.approvedAt).toLocaleString()}. Approval does not change the calculated verdict.
        </p>
      ) : null}
      <p className="mt-2 inline-block border border-stone-300 px-3 py-1 text-sm">
        {investigation.processingStatus === "Completed" && investigation.verdict ? investigation.verdict : statusLabel(investigation.processingStatus)}
      </p>
      {investigation.processingStatus === "Completed" && investigation.confidenceScore != null ? (
        <p className="mt-4 max-w-2xl text-stone-700">
          System confidence {investigation.confidenceScore.toFixed(2)}. This is not the probability the claim is true.
        </p>
      ) : null}
      {investigation.summary ? <p className="mt-4 max-w-2xl text-stone-700">{investigation.summary}</p> : null}
      {investigation.verdict === "InsufficientEvidence" ? (
        <p className="mt-4 max-w-2xl border border-stone-300 bg-white px-4 py-3 text-sm">
          No sources were strong enough to classify this claim.
        </p>
      ) : null}
      <p className="mt-4 max-w-2xl text-stone-700">{statusExplanation(investigation.processingStatus, investigation.failureCode)}</p>
      {investigation.evidenceProvider === "mock" ? (
        <p className="mt-4 border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-950" role="status">
          These sources are development fixtures, not live web search.
        </p>
      ) : null}
      {(investigation.claims ?? []).length > 0 ? (
        <section className="mt-8">
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
      <p className="mt-2 whitespace-pre-wrap border border-stone-200 bg-white px-4 py-3">{investigation.originalInput}</p>
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
      <div className="mt-6 flex gap-4 text-sm">
        <Link to="/app/investigations">Back to history</Link>
        {sessionUserId() === investigation.ownerId && investigation.processingStatus !== "Completed" && investigation.processingStatus !== "Failed" ? (
          <button type="button" onClick={() => void cancel()}>
            Cancel
          </button>
        ) : null}
        {sessionUserId() === investigation.ownerId && (investigation.processingStatus === "Failed" || investigation.processingStatus === "Completed") ? (
          <button type="button" onClick={() => void rerun()}>
            Re-run
          </button>
        ) : null}
        {sessionUserId() === investigation.ownerId ? (
          <button className="text-red-800" type="button" onClick={() => void remove()}>
            Delete
          </button>
        ) : null}
      </div>
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
    ["Supporting", items.filter((item) => item.stance === "Supporting")],
    ["Contradicting", items.filter((item) => item.stance === "Contradicting")],
    ["Context", items.filter((item) => item.stance === "Neutral" || item.stance == null)],
  ] as const;

  return (
    <div className="mt-3 space-y-4">
      {groups.map(([label, group]) =>
        group.length === 0 ? null : (
          <section key={label}>
            <h3 className="text-sm font-medium">{label}</h3>
            <ul className="mt-2 space-y-3">
              {group.map((item) => (
                <li key={item.id} className="border border-stone-200 bg-white px-4 py-3">
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
      <p className="text-sm text-stone-600">A reviewed stance does not replace the calculated verdict.</p>
      <label className="flex flex-col gap-1 text-sm text-stone-700">
        Stance
        <select
          className="border border-stone-300 px-3 py-2"
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
          className="border border-stone-300 px-3 py-2"
          value={note}
          onChange={(event) => setNote(event.target.value)}
          required
          maxLength={1000}
        />
      </label>
      <div className="flex gap-3 text-sm">
        <button className="border border-stone-400 px-3 py-2" type="submit" disabled={pending}>
          {confirming ? "Confirm stance" : "Override stance"}
        </button>
        <button className="border border-stone-400 px-3 py-2" type="button" disabled={pending} onClick={() => void flagSource()}>
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
      className="mt-8 max-w-xl border border-stone-200 bg-white px-4 py-3"
      onSubmit={(event) => {
        event.preventDefault();
        void approve();
      }}
    >
      <h2 className="text-lg font-medium">Approve investigation</h2>
      <label className="mt-3 flex flex-col gap-1 text-sm text-stone-700">
        Note
        <textarea className="border border-stone-300 px-3 py-2" value={note} onChange={(event) => setNote(event.target.value)} maxLength={1000} />
      </label>
      <button className="mt-3 border border-stone-400 px-3 py-2 text-sm" type="submit" disabled={pending}>
        Approve
      </button>
    </form>
  );
}

function statusLabel(status: string): string {
  if (status === "Pending") {
    return "Waiting to extract claims";
  }
  if (status === "ExtractingClaims") {
    return "Extracting claims";
  }
  if (status === "GatheringEvidence") {
    return "Collecting evidence";
  }
  if (status === "AnalyzingEvidence") {
    return "Classifying evidence";
  }
  if (status === "GeneratingConclusion") {
    return "Calculating verdict";
  }
  if (status === "Completed") {
    return "Completed";
  }
  if (status === "Failed") {
    return "Could not finish";
  }
  return status;
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
