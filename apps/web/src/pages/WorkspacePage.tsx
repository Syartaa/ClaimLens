import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, type AuthUser } from "../auth/api";
import { clearSession, getRefreshToken, hasSession } from "../auth/session";

export function WorkspacePage() {
  const navigate = useNavigate();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!hasSession()) {
      navigate("/login", { replace: true });
      return;
    }

    const controller = new AbortController();
    api<AuthUser>("/api/auth/me", { signal: controller.signal }, true)
      .then(setUser)
      .catch(() => {
        if (!controller.signal.aborted) {
          clearSession();
          setError("Your session ended. Sign in again.");
        }
      });

    return () => controller.abort();
  }, [navigate]);

  async function logout() {
    const refreshToken = getRefreshToken();
    try {
      if (refreshToken) {
        await api("/api/auth/logout", {
          method: "POST",
          body: JSON.stringify({ refreshToken }),
        }, true);
      }
    } finally {
      clearSession();
      navigate("/login");
    }
  }

  return (
    <main className="mx-auto flex min-h-svh max-w-3xl flex-col px-6 py-16">
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium tracking-wide text-stone-500">ClaimLens</p>
        <button className="text-sm text-stone-700" type="button" onClick={() => void logout()}>
          Sign out
        </button>
      </div>
      <h1 className="mt-8 text-3xl font-semibold tracking-tight text-stone-900">Workspace</h1>
      {user ? (
        <p className="mt-4 text-stone-700">
          Signed in as {user.firstName} {user.lastName} ({user.email}). Investigations arrive in the next phase.
        </p>
      ) : (
        <p className="mt-4 text-stone-600" role="status">
          {error ?? "Checking your session"}
        </p>
      )}
      {error ? (
        <Link className="mt-4 text-sm text-stone-800" to="/login">
          Sign in
        </Link>
      ) : null}
    </main>
  );
}
