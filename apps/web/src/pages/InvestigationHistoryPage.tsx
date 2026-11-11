import { type FormEvent, useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ApiError, api } from "../auth/api";
import type { InvestigationList } from "../investigations/types";

export function InvestigationHistoryPage() {
  const [params, setParams] = useSearchParams();
  const [list, setList] = useState<InvestigationList | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState(params.get("q") ?? "");
  const [refresh, setRefresh] = useState(0);

  const page = params.get("page") ?? "1";
  const inputType = params.get("inputType") ?? "";
  const processingStatus = params.get("processingStatus") ?? "";
  const verdict = params.get("verdict") ?? "";
  const sort = params.get("sort") ?? "-createdAt";

  useEffect(() => {
    const query = new URLSearchParams({ page, pageSize: "10" });
    const q = params.get("q");
    if (q) {
      query.set("q", q);
    }
    if (inputType) {
      query.set("inputType", inputType);
    }
    if (processingStatus) {
      query.set("processingStatus", processingStatus);
    }
    if (verdict) {
      query.set("verdict", verdict);
    }
    if (sort) {
      query.set("sort", sort);
    }

    api<InvestigationList>(`/api/investigations?${query.toString()}`, {}, true)
      .then(setList)
      .catch((caught: unknown) => {
        setError(caught instanceof ApiError ? caught.message : "Could not load investigations.");
      });
  }, [page, inputType, processingStatus, verdict, sort, params, refresh]);

  async function remove(id: string) {
    if (!window.confirm("Delete this investigation?")) {
      return;
    }

    try {
      await api(`/api/investigations/${id}`, { method: "DELETE" }, true);
      setRefresh((value) => value + 1);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : "Something went wrong. Try again.");
    }
  }

  async function rerun(id: string) {
    try {
      await api(`/api/investigations/${id}/reanalyze`, { method: "POST" }, true);
      setRefresh((value) => value + 1);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : "Something went wrong. Try again.");
    }
  }

  function applySearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const next = new URLSearchParams(params);
    if (search.trim()) {
      next.set("q", search.trim());
    } else {
      next.delete("q");
    }
    next.set("page", "1");
    setParams(next);
  }

  function setFilter(name: string, value: string) {
    const next = new URLSearchParams(params);
    if (value) {
      next.set(name, value);
    } else {
      next.delete(name);
    }
    next.set("page", "1");
    setParams(next);
  }

  const totalPages = list ? Math.max(1, Math.ceil(list.totalCount / list.pageSize)) : 1;

  return (
    <section>
      <h1 className="text-3xl font-semibold tracking-tight">Investigation history</h1>
      <form className="mt-6 flex flex-wrap gap-3" onSubmit={applySearch}>
        <label className="text-sm text-stone-700">
          Search
          <input
            className="ml-2 border border-stone-300 px-3 py-2"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </label>
        <label className="text-sm text-stone-700">
          Status
          <select
            className="ml-2 border border-stone-300 px-3 py-2"
            value={processingStatus}
            onChange={(event) => setFilter("processingStatus", event.target.value)}
          >
            <option value="">Any</option>
            <option value="Pending">Pending</option>
            <option value="Completed">Completed</option>
            <option value="Failed">Failed</option>
          </select>
        </label>
        <label className="text-sm text-stone-700">
          Input
          <select
            className="ml-2 border border-stone-300 px-3 py-2"
            value={inputType}
            onChange={(event) => setFilter("inputType", event.target.value)}
          >
            <option value="">Any</option>
            <option value="Claim">Claim</option>
            <option value="ArticleText">Article text</option>
          </select>
        </label>
        <label className="text-sm text-stone-700">
          Verdict
          <select
            className="ml-2 border border-stone-300 px-3 py-2"
            value={verdict}
            onChange={(event) => setFilter("verdict", event.target.value)}
          >
            <option value="">Any</option>
            <option value="Supported">Supported</option>
            <option value="MostlySupported">Mostly supported</option>
            <option value="Mixed">Mixed</option>
            <option value="MostlyContradicted">Mostly contradicted</option>
            <option value="Contradicted">Contradicted</option>
            <option value="InsufficientEvidence">Insufficient evidence</option>
          </select>
        </label>
        <label className="text-sm text-stone-700">
          Sort
          <select
            className="ml-2 border border-stone-300 px-3 py-2"
            value={sort}
            onChange={(event) => setFilter("sort", event.target.value)}
          >
            <option value="-createdAt">Newest</option>
            <option value="createdAt">Oldest</option>
            <option value="-updatedAt">Recently updated</option>
          </select>
        </label>
        <button className="border border-stone-300 px-4 py-2 text-sm" type="submit">
          Apply
        </button>
      </form>
      {error ? (
        <p className="mt-6 border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-900" role="alert">
          {error}
        </p>
      ) : null}
      {!list && !error ? <p className="mt-6" role="status">Loading history</p> : null}
      {list && list.items.length === 0 ? (
        <p className="mt-6 text-stone-600">
          No investigations match these filters. <Link to="/app/investigations/new">Start one</Link>.
        </p>
      ) : null}
      <ul className="mt-6 divide-y divide-stone-200 border border-stone-200 bg-white">
        {list?.items.map((item) => (
          <li key={item.id} className="flex items-center justify-between gap-4 px-4 py-3">
            <Link to={`/app/investigations/${item.id}`}>
              <span className="block">{item.title}</span>
              <span className="text-sm text-stone-500">{item.inputType}</span>
            </Link>
            <span className="text-sm text-stone-500">{item.verdict ?? item.processingStatus}</span>
            <span className="flex gap-3 text-sm">
              <button type="button" onClick={() => void rerun(item.id)}>
                Re-run
              </button>
              <button className="text-red-800" type="button" onClick={() => void remove(item.id)}>
                Delete
              </button>
            </span>
          </li>
        ))}
      </ul>
      <div className="mt-4 flex items-center gap-3 text-sm">
        <button
          className="border border-stone-300 px-3 py-1 disabled:opacity-40"
          type="button"
          disabled={page === "1"}
          onClick={() => setFilter("page", String(Number(page) - 1))}
        >
          Previous
        </button>
        <span>
          Page {page} of {totalPages}
        </span>
        <button
          className="border border-stone-300 px-3 py-1 disabled:opacity-40"
          type="button"
          disabled={Number(page) >= totalPages}
          onClick={() => setFilter("page", String(Number(page) + 1))}
        >
          Next
        </button>
      </div>
    </section>
  );
}
