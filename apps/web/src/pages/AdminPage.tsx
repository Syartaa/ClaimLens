import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiError, api } from "../auth/api";

type CountByLabel = { label: string; count: number };

type Dashboard = {
  users: number;
  activeUsers: number;
  investigations: number;
  inProgress: number;
  completed: number;
  failed: number;
  aiRuns: number;
  aiRunFailures: number;
  failureCodes: CountByLabel[];
  providers: CountByLabel[];
  models: CountByLabel[];
};

type UserList = {
  items: Array<{
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    role: string;
    isActive: boolean;
    createdAt: string;
  }>;
};

type InvestigationList = {
  items: Array<{
    id: string;
    title: string;
    ownerEmail: string;
    failureCode: string | null;
    evidenceProvider: string | null;
  }>;
};

type AiRunList = {
  items: Array<{
    id: string;
    investigationId: string;
    purpose: string;
    modelUsed: string;
    errorCategory: string | null;
    createdAt: string;
  }>;
};

export function AdminPage() {
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [users, setUsers] = useState<UserList | null>(null);
  const [failures, setFailures] = useState<InvestigationList | null>(null);
  const [aiErrors, setAiErrors] = useState<AiRunList | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([
      api<Dashboard>("/api/admin/dashboard", {}, true),
      api<UserList>("/api/admin/users", {}, true),
      api<InvestigationList>("/api/admin/investigations?processingStatus=Failed", {}, true),
      api<AiRunList>("/api/admin/ai-runs?status=Failed", {}, true),
    ])
      .then(([counts, people, failedRuns, failedAi]) => {
        setDashboard(counts);
        setUsers(people);
        setFailures(failedRuns);
        setAiErrors(failedAi);
      })
      .catch((caught: unknown) => {
        setError(caught instanceof ApiError ? caught.message : "The admin dashboard could not be loaded.");
      });
  }, []);

  if (error) {
    return (
      <p className="border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-900" role="alert">
        {error}
      </p>
    );
  }

  if (!dashboard || !users || !failures || !aiErrors) {
    return <p role="status">Loading admin dashboard</p>;
  }

  return (
    <section>
      <h1 className="text-3xl font-semibold tracking-tight">Admin</h1>
      <p className="mt-3 max-w-2xl text-stone-700">
        System monitoring. These counts describe runs and providers. They are not a measure of whether a claim is true.
      </p>
      <dl className="mt-8 grid gap-3 sm:grid-cols-4">
        <Count label="Users" value={dashboard.users} />
        <Count label="Active users" value={dashboard.activeUsers} />
        <Count label="Investigations" value={dashboard.investigations} />
        <Count label="In progress" value={dashboard.inProgress} />
        <Count label="Completed" value={dashboard.completed} />
        <Count label="Failed" value={dashboard.failed} />
        <Count label="AI runs" value={dashboard.aiRuns} />
        <Count label="AI run errors" value={dashboard.aiRunFailures} />
      </dl>

      <h2 className="mt-10 text-lg font-medium">Provider usage</h2>
      {dashboard.providers.length === 0 ? (
        <p className="mt-3 text-sm text-stone-600">No evidence provider has been recorded yet.</p>
      ) : (
        <ul className="mt-3 flex flex-wrap gap-2">
          {dashboard.providers.map((item) => (
            <li key={item.label} className="border border-stone-300 bg-white px-3 py-2 text-sm">
              {item.label} · {item.count}
            </li>
          ))}
        </ul>
      )}
      {dashboard.models.length > 0 ? (
        <ul className="mt-3 flex flex-wrap gap-2">
          {dashboard.models.map((item) => (
            <li key={item.label} className="border border-stone-200 bg-white px-3 py-2 text-sm text-stone-600">
              Model {item.label || "unspecified"} · {item.count}
            </li>
          ))}
        </ul>
      ) : null}

      <h2 className="mt-10 text-lg font-medium">Users</h2>
      <ul className="mt-3 divide-y divide-stone-200 border border-stone-200 bg-white">
        {users.items.map((user) => (
          <li key={user.id} className="flex flex-wrap items-baseline justify-between gap-3 px-4 py-3">
            <div>
              <p className="font-medium">{user.email}</p>
              <p className="text-sm text-stone-500">
                {user.firstName} {user.lastName} · {user.role}
                {user.isActive ? "" : " · inactive"}
              </p>
            </div>
            <p className="text-sm text-stone-500">{new Date(user.createdAt).toLocaleDateString()}</p>
          </li>
        ))}
      </ul>

      <h2 className="mt-10 text-lg font-medium">Failed investigations</h2>
      {dashboard.failureCodes.length > 0 ? (
        <ul className="mt-3 flex flex-wrap gap-2">
          {dashboard.failureCodes.map((item) => (
            <li key={item.label} className="border border-stone-300 bg-white px-3 py-2 text-sm">
              {item.label} · {item.count}
            </li>
          ))}
        </ul>
      ) : null}
      {failures.items.length === 0 ? (
        <p className="mt-3 text-sm text-stone-600">No failed investigations.</p>
      ) : (
        <ul className="mt-3 divide-y divide-stone-200 border border-stone-200 bg-white">
          {failures.items.map((item) => (
            <li key={item.id} className="px-4 py-3">
              <Link className="font-medium" to={`/app/investigations/${item.id}`}>
                {item.title}
              </Link>
              <p className="text-sm text-stone-500">
                {item.ownerEmail}
                {item.failureCode ? ` · ${item.failureCode}` : ""}
                {item.evidenceProvider ? ` · ${item.evidenceProvider}` : ""}
              </p>
            </li>
          ))}
        </ul>
      )}

      <h2 className="mt-10 text-lg font-medium">AI run errors</h2>
      {aiErrors.items.length === 0 ? (
        <p className="mt-3 text-sm text-stone-600">No failed AI runs.</p>
      ) : (
        <ul className="mt-3 divide-y divide-stone-200 border border-stone-200 bg-white">
          {aiErrors.items.map((item) => (
            <li key={item.id} className="px-4 py-3">
              <Link className="font-medium" to={`/app/investigations/${item.investigationId}`}>
                {item.purpose}
              </Link>
              <p className="text-sm text-stone-500">
                {item.errorCategory ?? "Failed"} · {item.modelUsed} · {new Date(item.createdAt).toLocaleString()}
              </p>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function Count({ label, value }: { label: string; value: number }) {
  return (
    <div className="border border-stone-200 bg-white px-4 py-3">
      <dt className="text-sm text-stone-500">{label}</dt>
      <dd className="mt-1 text-2xl font-semibold">{value}</dd>
    </div>
  );
}
