import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ApiError, api } from "../auth/api";
import { hasSession } from "../auth/session";
import type { InvestigationList, InvestigationSummary } from "../investigations/types";
import { labelFor, Pill, primaryButton } from "../ui";

export function WorkspaceHomePage() {
  const navigate = useNavigate();
  const [list, setList] = useState<InvestigationList | null>(null);
  const [summary, setSummary] = useState<InvestigationSummary | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!hasSession()) {
      navigate("/login", { replace: true });
      return;
    }

    Promise.all([
      api<InvestigationSummary>("/api/investigations/summary", {}, true),
      api<InvestigationList>("/api/investigations?pageSize=5", {}, true),
    ])
      .then(([counts, recent]) => {
        setSummary(counts);
        setList(recent);
      })
      .catch((caught: unknown) => {
        setError(caught instanceof ApiError ? caught.message : "Something went wrong. Try again.");
      });
  }, [navigate]);

  return (
    <section>
      <div className="flex items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight">Workspace</h1>
          <p className="mt-2 max-w-2xl text-stone-600">
            Verdicts come from the evidence rules. System confidence is not the probability a claim is true.
          </p>
        </div>
        <Link className={primaryButton} to="/app/investigations/new">
          New investigation
        </Link>
      </div>
      {error ? (
        <p className="mt-6 border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-900" role="alert">
          {error}
        </p>
      ) : null}
      {!summary && !error ? <p className="mt-8" role="status">Loading workspace</p> : null}
      {summary ? (
        <dl className="mt-8 grid gap-3 sm:grid-cols-5">
          <Count label="Total" value={summary.total} />
          <Count label="Completed" value={summary.completed} />
          <Count label="In progress" value={summary.inProgress} />
          <Count label="Failed" value={summary.failed} />
          <Count label="Claims analyzed" value={summary.claimsAnalyzed} />
        </dl>
      ) : null}
      {summary && summary.verdicts.length > 0 ? (
        <section className="mt-8">
          <h2 className="text-lg font-medium">Verdicts among completed investigations</h2>
          <ul className="mt-3 flex flex-wrap gap-2">
            {summary.verdicts.map((item) => (
              <li key={item.verdict}>
                <Pill value={item.verdict}>{`${labelFor(item.verdict)} · ${item.count}`}</Pill>
              </li>
            ))}
          </ul>
        </section>
      ) : null}
      <h2 className="mt-10 text-lg font-medium">Recent</h2>
      {list && list.items.length === 0 ? (
        <div className="mt-3 rounded-lg border border-teal-200 bg-teal-50 px-4 py-4 text-teal-950">
          <p>No investigations yet. Start with one claim.</p>
          <Link className={`${primaryButton} mt-3`} to="/app/investigations/new">
            Start an investigation
          </Link>
        </div>
      ) : null}
      <ul className="mt-3 divide-y divide-stone-200 border border-stone-200 bg-white">
        {list?.items.map((item) => (
          <li key={item.id}>
            <Link className="flex items-center justify-between gap-3 px-4 py-3 hover:bg-teal-50" to={`/app/investigations/${item.id}`}>
              <span>{item.title}</span>
              <Pill value={item.verdict ?? item.processingStatus} />
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}

function Count({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border border-stone-200 border-t-4 border-t-teal-700 bg-white px-4 py-3">
      <dt className="font-sans text-sm text-stone-500">{label}</dt>
      <dd className="mt-1 text-2xl font-semibold">{value}</dd>
    </div>
  );
}
