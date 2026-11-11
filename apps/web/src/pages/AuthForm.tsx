import { type FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ApiError, api, type AuthResult } from "../auth/api";
import { setSession } from "../auth/session";

type AuthFormProps = {
  mode: "login" | "register";
};

export function AuthForm({ mode }: AuthFormProps) {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [details, setDetails] = useState<string[]>([]);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setError(null);
    setDetails([]);

    const path = mode === "login" ? "/api/auth/login" : "/api/auth/register";
    const body =
      mode === "login"
        ? { email, password }
        : { email, password, firstName, lastName };

    try {
      const result = await api<AuthResult>(path, {
        method: "POST",
        body: JSON.stringify(body),
      });
      setSession(result.accessToken, result.refreshToken);
      navigate("/app");
    } catch (caught) {
      if (caught instanceof ApiError) {
        setError(caught.message);
        setDetails(caught.details);
      } else {
        setError("Something went wrong. Try again.");
        setDetails([]);
      }
    } finally {
      setPending(false);
    }
  }

  const title = mode === "login" ? "Sign in" : "Create an account";

  return (
    <main className="mx-auto flex min-h-svh max-w-md flex-col justify-center px-6 py-16">
      <p className="text-sm font-medium tracking-wide text-stone-500">ClaimLens</p>
      <h1 className="mt-3 text-3xl font-semibold tracking-tight text-stone-900">{title}</h1>
      {error ? <FormAlert message={error} details={details} /> : null}
      <form className="mt-8 flex flex-col gap-4" onSubmit={onSubmit}>
        {mode === "register" ? (
          <>
            <label className="flex flex-col gap-1 text-sm text-stone-700">
              First name
              <input
                className="border border-stone-300 px-3 py-2 text-stone-900"
                autoComplete="given-name"
                value={firstName}
                onChange={(event) => setFirstName(event.target.value)}
                required
              />
            </label>
            <label className="flex flex-col gap-1 text-sm text-stone-700">
              Last name
              <input
                className="border border-stone-300 px-3 py-2 text-stone-900"
                autoComplete="family-name"
                value={lastName}
                onChange={(event) => setLastName(event.target.value)}
                required
              />
            </label>
          </>
        ) : null}
        <label className="flex flex-col gap-1 text-sm text-stone-700">
          Email
          <input
            className="border border-stone-300 px-3 py-2 text-stone-900"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />
        </label>
        <label className="flex flex-col gap-1 text-sm text-stone-700">
          Password
          <input
            className="border border-stone-300 px-3 py-2 text-stone-900"
            type="password"
            autoComplete={mode === "login" ? "current-password" : "new-password"}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
        </label>
        {mode === "register" ? (
          <p className="text-sm text-stone-500">Use at least 8 characters, with upper and lower case letters and a digit.</p>
        ) : null}
        <button className="bg-stone-900 px-4 py-2 text-sm text-white disabled:opacity-60" type="submit" disabled={pending}>
          {pending ? "Please wait" : title}
        </button>
      </form>
      <p className="mt-6 text-sm text-stone-600">
        {mode === "login" ? (
          <Link to="/register">Create an account</Link>
        ) : (
          <Link to="/login">Already have an account? Sign in</Link>
        )}
      </p>
    </main>
  );
}

function FormAlert({ message, details }: { message: string; details: string[] }) {
  return (
    <div className="mt-6 border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-900" role="alert">
      <p>{message}</p>
      {details.length > 0 ? (
        <ul className="mt-2 list-disc pl-5">
          {details.map((detail) => (
            <li key={detail}>{detail}</li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
