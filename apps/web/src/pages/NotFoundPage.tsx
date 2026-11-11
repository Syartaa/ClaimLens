import { Link } from "react-router-dom";

export function NotFoundPage() {
  return (
    <main className="mx-auto max-w-xl px-6 py-24">
      <h1 className="text-3xl font-semibold tracking-tight">Page not found</h1>
      <p className="mt-3 text-stone-600">That address is not part of ClaimLens.</p>
      <Link className="mt-6 inline-block text-sm" to="/">
        Back to the start
      </Link>
    </main>
  );
}
