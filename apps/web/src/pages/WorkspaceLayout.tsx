import { useEffect } from "react";
import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import { api } from "../auth/api";
import { canReview, clearSession, getRefreshToken, hasSession, isAdmin } from "../auth/session";
import { primaryButton, secondaryButton } from "../ui";

export function WorkspaceLayout() {
  const navigate = useNavigate();

  useEffect(() => {
    if (!hasSession()) {
      navigate("/login", { replace: true });
    }
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
    <div className="min-h-svh bg-stone-50 text-stone-900">
      <header className="border-b border-stone-200 bg-white">
        <div className="mx-auto flex max-w-5xl flex-wrap items-center gap-3 px-6 py-4">
          <Link className="text-sm font-medium tracking-wide text-stone-500" to="/app">
            ClaimLens
          </Link>
          <nav className="flex flex-wrap items-center gap-2 text-sm">
            <NavLink className={navClass} to="/app" end>
              Workspace
            </NavLink>
            <NavLink className={navClass} to="/app/investigations" end>
              History
            </NavLink>
            <NavLink className={navClass} to="/app/investigations/new">
              New investigation
            </NavLink>
            {canReview() ? (
              <NavLink className={navClass} to="/app/review">
                Review
              </NavLink>
            ) : null}
            {isAdmin() ? (
              <NavLink className={navClass} to="/app/admin">
                Admin
              </NavLink>
            ) : null}
          </nav>
          <button className={`${secondaryButton} ml-auto`} type="button" onClick={() => void logout()}>
            Sign out
          </button>
        </div>
      </header>
      <div className="mx-auto max-w-5xl px-6 py-10">
        <Outlet />
      </div>
    </div>
  );
}

function navClass({ isActive }: { isActive: boolean }) {
  return isActive ? primaryButton : secondaryButton;
}
