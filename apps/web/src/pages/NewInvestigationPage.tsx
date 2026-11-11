import { type FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, api } from "../auth/api";
import type { Investigation } from "../investigations/types";
import { fieldClass, primaryButton } from "../ui";

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
      <p className="mt-3 text-stone-600">Choose one input, paste the text, then start. The next page shows each step.</p>
      <form className="mt-8 flex flex-col gap-4" onSubmit={onSubmit}>
        <fieldset>
          <legend className="mb-2 text-sm text-stone-700">Input</legend>
          <div className="grid gap-3 sm:grid-cols-3">
            <Choice
              selected={inputType === "Claim"}
              title="Claim"
              detail="One sentence you want checked."
              onSelect={() => setInputType("Claim")}
            />
            <Choice
              selected={inputType === "ArticleText"}
              title="Article text"
              detail="A few sentences. Several claims may be found."
              onSelect={() => setInputType("ArticleText")}
            />
            <div className="rounded-lg border border-dashed border-stone-300 bg-stone-100 px-4 py-3 text-stone-400">
              <p className="font-sans text-sm font-medium">URL</p>
              <p className="mt-1 text-sm">Coming later</p>
            </div>
          </div>
        </fieldset>
        <label className="flex flex-col gap-1 text-sm text-stone-700">
          {inputType === "Claim" ? "Claim" : "Article text"}
          <textarea
            className={`min-h-40 ${fieldClass}`}
            value={text}
            onChange={(event) => setText(event.target.value)}
            required
            placeholder={inputType === "Claim" ? "Vitamin C prevents the common cold." : "Paste two or three sentences from an article."}
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
        <button className={`${primaryButton} w-fit`} type="submit" disabled={pending}>
          {pending ? "Saving" : "Analyze claim"}
        </button>
      </form>
    </section>
  );
}

function Choice({
  selected,
  title,
  detail,
  onSelect,
}: {
  selected: boolean;
  title: string;
  detail: string;
  onSelect: () => void;
}) {
  return (
    <button
      type="button"
      aria-pressed={selected}
      className={`rounded-lg border px-4 py-3 text-left ${selected ? "border-teal-700 bg-teal-50 text-teal-950" : "border-stone-300 bg-white text-stone-800 hover:border-teal-600"}`}
      onClick={onSelect}
    >
      <span className="block font-sans text-sm font-medium">{title}</span>
      <span className="mt-1 block text-sm">{detail}</span>
    </button>
  );
}
