const accessKey = "claimlens.accessToken";
const refreshKey = "claimlens.refreshToken";

export type SessionUser = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
};

export function getAccessToken(): string | null {
  return sessionStorage.getItem(accessKey);
}

export function getRefreshToken(): string | null {
  return sessionStorage.getItem(refreshKey);
}

export function setSession(accessToken: string, refreshToken: string): void {
  sessionStorage.setItem(accessKey, accessToken);
  sessionStorage.setItem(refreshKey, refreshToken);
}

export function clearSession(): void {
  sessionStorage.removeItem(accessKey);
  sessionStorage.removeItem(refreshKey);
}

export function hasSession(): boolean {
  return getAccessToken() !== null && getRefreshToken() !== null;
}

export function sessionRole(): string | null {
  const role = readClaims()?.role;
  return typeof role === "string" ? role : null;
}

export function sessionUserId(): string | null {
  const subject = readClaims()?.sub;
  return typeof subject === "string" ? subject : null;
}

export function canReview(): boolean {
  const role = sessionRole();
  return role === "Reviewer" || role === "Admin";
}

export function isAdmin(): boolean {
  return sessionRole() === "Admin";
}

function readClaims(): Record<string, unknown> | null {
  const token = getAccessToken();
  const segment = token?.split(".")[1];
  if (!segment) {
    return null;
  }

  try {
    const padded = segment.replace(/-/g, "+").replace(/_/g, "/");
    const json = atob(padded.padEnd(padded.length + ((4 - (padded.length % 4)) % 4), "="));
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}
