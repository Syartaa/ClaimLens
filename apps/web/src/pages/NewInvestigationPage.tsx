import { type FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, api } from "../auth/api";
import type { Investigation } from "../investigations/types";

type InputChoice = "Claim" | "ArticleText";

export function NewInvestigationPage() {
  const navigate = useNavigate();
  const [inputType, setInputType] = useState<InputChoice>("Claim");
  const [text, setText] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [details, setDetails] = useState<string[]>([]);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setError(null);
    setDetails([]);

    try {
      const created = await api<Investigation>("/api/investigations", {
        method: "POST",
        body: JSON.stringify({ inputType, text }),
      }, true);
      navigate(`/app/investigations/${created.id}`);
    } catch (caught) {
      if (caught instanceof ApiError) {
        setError(caught.message);
        setDetails(caught.details);
      } else {
        setError("The investigation could not be created.");
      }
    } finally {
      setPending(false);
    }
  }

  return (
    <section className="max-w-2xl">
      <h1 className="text-3xl font-semibold tracking-tight">What would you like to investigate?</h1>
      <form className="mt-8 flex flex-col gap-4" onSubmit={onSubmit}>
        <fieldset className="flex gap-4 text-sm">
          <legend className="mb-2 text-stone-700">Input</legend>
          <label className="flex items-center gap-2">
            <input
              type="radio"
              name="inputType"
              checked={inputType === "Claim"}
              onChange={() => setInputType("Claim")}
            />
            Claim
          </label>
          <label className="flex items-center gap-2">
            <input
              type="radio"
              name="inputType"
              checked={inputType === "ArticleText"}
              onChange={() => setInputType("ArticleText")}
            />
            Article text
          </label>
          <label className="flex items-center gap-2 text-stone-400">
            <input type="radio" name="inputType" disabled />
            URL (coming later)
          </label>
        </fieldset>
        <label className="flex flex-col gap-1 text-sm text-stone-700">
          {inputType === "Claim" ? "Claim" : "Article text"}
          <textarea
            className="min-h-40 border border-stone-300 px-3 py-2 text-stone-900"
            value={text}
            onChange={(event) => setText(event.target.value)}
            required
          />
        </label>
        {error ? (
          <div className="border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-900" role="alert">
            <p>{error}</p>
            {details.length > 0 ? (
              <ul className="mt-2 list-disc pl-5">
                {details.map((detail) => (
                  <li key={detail}>{detail}</li>
                ))}
              </ul>
            ) : null}
          </div>
        ) : null}
        <button className="w-fit bg-stone-900 px-4 py-2 text-sm text-white disabled:opacity-60" type="submit" disabled={pending}>
          {pending ? "Saving" : "Analyze claim"}
        </button>
      </form>
    </section>
  );
}
