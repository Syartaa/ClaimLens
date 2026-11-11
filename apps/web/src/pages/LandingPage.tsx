import { Link } from "react-router-dom";
import { useEffect, useState } from "react";
import { baseUrl } from "../auth/api";
import { healthLabel } from "../lib/health";
import { primaryButton, secondaryButton } from "../ui";

type ServiceHealth = {
  status: string;
  service: string;
};

export function LandingPage() {
  const [status, setStatus] = useState<string | undefined>(undefined);

  useEffect(() => {
    const controller = new AbortController();

    fetch(`${baseUrl}/api/health`, { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error("unhealthy");
        }

        return (await response.json()) as ServiceHealth;
      })
      .then((body) => {
        setStatus(body.status);
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setStatus("unavailable");
      });

    return () => controller.abort();
  }, []);

  return (
    <main className="mx-auto flex min-h-svh max-w-3xl flex-col justify-center px-6 py-16">
      <p className="text-sm font-medium tracking-wide text-stone-500">ClaimLens</p>
      <h1 className="mt-3 text-4xl font-semibold tracking-tight text-stone-900">
        Investigate claims. Compare evidence. Understand the context.
      </h1>
      <p className="mt-4 max-w-xl text-lg text-stone-600">
        Evidence comes before a conclusion. Create an account to start an investigation workspace.
      </p>
      <div className="mt-8 flex flex-wrap gap-3">
        <Link className={primaryButton} to="/register">
          Create account
        </Link>
        <Link className={secondaryButton} to="/login">
          Sign in
        </Link>
      </div>
      <ol className="mt-10 grid gap-3 sm:grid-cols-3">
        <li className="rounded-lg border border-teal-200 bg-teal-50 px-4 py-3 text-sm text-teal-950">
          <span className="font-sans text-xs font-medium uppercase tracking-wide">1. Submit</span>
          <p className="mt-1">Paste a claim or a short article.</p>
        </li>
        <li className="rounded-lg border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-950">
          <span className="font-sans text-xs font-medium uppercase tracking-wide">2. Wait</span>
          <p className="mt-1">The page follows each step until it finishes.</p>
        </li>
        <li className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-950">
          <span className="font-sans text-xs font-medium uppercase tracking-wide">3. Read</span>
          <p className="mt-1">The verdict comes from the evidence rules.</p>
        </li>
      </ol>
      <p className="mt-8 text-sm text-stone-800" role="status">
        {healthLabel(status)}
      </p>
    </main>
  );
}
