import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiError, api } from "../auth/api";
import { labelFor, Pill, secondaryButton } from "../ui";

type ReviewQueue = {
  items: Array<{
    id: string;
    title: string;
    verdict: string | null;
    completedAt: string | null;
    approvedAt: string | null;
    ownerEmail: string;
  }>;
  totalCount: number;
};

export function ReviewQueuePage() {
  const [queue, setQueue] = useState<ReviewQueue | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<ReviewQueue>("/api/reviews/queue", {}, true)
      .then(setQueue)
      .catch((caught: unknown) => {
        setError(caught instanceof ApiError ? caught.message : "The review queue could not be loaded.");
      });
  }, []);

  if (error) {
    return (
      <p className="border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-900" role="alert">
        {error}
      </p>
    );
  }

  if (!queue) {
    return <p role="status">Loading review queue</p>;
  }

  return (
    <section>
      <h1 className="text-3xl font-semibold tracking-tight">Review queue</h1>
      <p className="mt-3 max-w-2xl text-stone-700">
        Completed investigations. Confirming or overriding a stance does not change the calculated verdict.
      </p>
      {queue.items.length === 0 ? (
        <p className="mt-8 border border-stone-200 bg-white px-4 py-6 text-sm text-stone-600">
          No completed investigations are waiting.
        </p>
      ) : (
        <ul className="mt-8 divide-y divide-stone-200 overflow-hidden rounded-lg border border-stone-200 bg-white">
          {queue.items.map((item) => (
            <li key={item.id} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
              <div>
                <Link className="font-medium" to={`/app/investigations/${item.id}`}>
                  {item.title}
                </Link>
                <p className="text-sm text-stone-500">
                  {item.ownerEmail}
                  {item.verdict ? ` · ${labelFor(item.verdict)}` : ""}
                  {item.completedAt ? ` · ${new Date(item.completedAt).toLocaleString()}` : ""}
                </p>
              </div>
              <div className="flex items-center gap-2">
                <Pill value={item.approvedAt ? "Approved" : "NeedsReview"} />
                <Link className={secondaryButton} to={`/app/investigations/${item.id}`}>
                  Open
                </Link>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
