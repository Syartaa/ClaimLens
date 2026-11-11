import { clearSession, getAccessToken, getRefreshToken, setSession } from "./session";

export type AuthUser = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
};

export type AuthResult = {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
};

type ErrorBody = {
  code?: string;
  message?: string;
  title?: string;
  detail?: string;
  details?: Array<{ field?: string; message?: string }>;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  readonly code: string;
  readonly details: string[];

  constructor(message: string, code: string, details: string[] = []) {
    super(message);
    this.code = code;
    this.details = details;
  }
}

export function describeApiError(status: number, body: unknown): ApiError {
  const payload = isRecord(body) ? (body as ErrorBody) : {};
  const details = collectDetails(payload);
  const message =
    payload.message ??
    payload.detail ??
    payload.title ??
    fallbackMessage(status);

  return new ApiError(message, payload.code ?? fallbackCode(status), details);
}

const configuredApiUrl = import.meta.env.VITE_API_URL ?? "";
export const baseUrl = configuredApiUrl === "same-origin" ? "" : configuredApiUrl;

export async function api<T>(path: string, options: RequestInit = {}, authenticated = false): Promise<T> {
  const headers = new Headers(options.headers);
  if (options.body !== undefined && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (authenticated) {
    const accessToken = getAccessToken();
    if (accessToken) {
      headers.set("Authorization", `Bearer ${accessToken}`);
    }
  }

  const response = await fetch(`${baseUrl}${path}`, { ...options, headers });
  if (response.status === 401 && authenticated && path !== "/api/auth/refresh") {
    const refreshed = await refreshSession();
    if (refreshed) {
      headers.set("Authorization", `Bearer ${getAccessToken()}`);
      const retry = await fetch(`${baseUrl}${path}`, { ...options, headers });
      return read<T>(retry);
    }
  }

  return read<T>(response);
}

export async function refreshSession(): Promise<boolean> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) {
    clearSession();
    return false;
  }

  const response = await fetch(`${baseUrl}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });

  if (!response.ok) {
    clearSession();
    return false;
  }

  const body = (await response.json()) as AuthResult;
  setSession(body.accessToken, body.refreshToken);
  return true;
}

async function read<T>(response: Response): Promise<T> {
  if (response.ok) {
    if (response.status === 204) {
      return undefined as T;
    }

    return (await response.json()) as T;
  }

  try {
    throw describeApiError(response.status, await response.json());
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }

    throw new ApiError(fallbackMessage(response.status), fallbackCode(response.status));
  }
}

function collectDetails(body: ErrorBody): string[] {
  const fromDetails = (body.details ?? [])
    .map((detail) => detail.message)
    .filter((message): message is string => Boolean(message));

  const fromErrors = Object.values(body.errors ?? {}).flat();
  return [...fromDetails, ...fromErrors];
}

function fallbackMessage(status: number): string {
  if (status === 400) {
    return "Check the form and try again.";
  }

  if (status === 401) {
    return "Email or password is incorrect.";
  }

  if (status === 403) {
    return "You do not have access to that action.";
  }

  if (status === 409) {
    return "An account with this email already exists.";
  }

  return "Something went wrong. Try again.";
}

function fallbackCode(status: number): string {
  if (status === 400) {
    return "VALIDATION_FAILED";
  }

  if (status === 401) {
    return "UNAUTHORIZED";
  }

  return "INTERNAL_ERROR";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}
