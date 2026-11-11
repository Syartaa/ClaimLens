import { Link } from "react-router-dom";
import { useEffect, useState } from "react";
import { baseUrl } from "../auth/api";
import { healthLabel } from "../lib/health";

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
      <div className="mt-8 flex gap-3">
        <Link className="bg-stone-900 px-4 py-2 text-sm text-white" to="/register">
          Create account
        </Link>
        <Link className="border border-stone-300 px-4 py-2 text-sm text-stone-800" to="/login">
          Sign in
        </Link>
      </div>
      <p className="mt-8 text-sm text-stone-800" role="status">
        {healthLabel(status)}
      </p>
    </main>
  );
}
