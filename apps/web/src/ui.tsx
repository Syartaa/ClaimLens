export const primaryButton =
  "inline-flex items-center justify-center rounded-md bg-teal-800 px-4 py-2 text-sm font-medium font-sans text-white hover:bg-teal-900 disabled:cursor-not-allowed disabled:opacity-60";

export const secondaryButton =
  "inline-flex items-center justify-center rounded-md border border-stone-300 bg-white px-4 py-2 text-sm font-medium font-sans text-stone-800 hover:bg-stone-50 disabled:cursor-not-allowed disabled:opacity-40";

export const dangerButton =
  "inline-flex items-center justify-center rounded-md border border-rose-300 bg-rose-50 px-4 py-2 text-sm font-medium font-sans text-rose-900 hover:bg-rose-100 disabled:cursor-not-allowed disabled:opacity-60";

export const fieldClass =
  "rounded-md border border-stone-300 bg-white px-3 py-2 font-sans text-stone-900 outline-none focus:border-teal-700 focus:ring-2 focus:ring-teal-700/20";

const tones: Record<string, string> = {
  Supported: "bg-emerald-100 text-emerald-950 ring-emerald-300",
  MostlySupported: "bg-teal-100 text-teal-950 ring-teal-300",
  Mixed: "bg-amber-100 text-amber-950 ring-amber-300",
  MostlyContradicted: "bg-orange-100 text-orange-950 ring-orange-300",
  Contradicted: "bg-rose-100 text-rose-950 ring-rose-300",
  InsufficientEvidence: "bg-stone-200 text-stone-800 ring-stone-300",
  Completed: "bg-emerald-100 text-emerald-950 ring-emerald-300",
  Failed: "bg-rose-100 text-rose-950 ring-rose-300",
  Pending: "bg-sky-100 text-sky-950 ring-sky-300",
  ExtractingClaims: "bg-sky-100 text-sky-950 ring-sky-300",
  GatheringEvidence: "bg-sky-100 text-sky-950 ring-sky-300",
  AnalyzingEvidence: "bg-sky-100 text-sky-950 ring-sky-300",
  GeneratingConclusion: "bg-sky-100 text-sky-950 ring-sky-300",
  Reviewer: "bg-sky-100 text-sky-950 ring-sky-300",
  Admin: "bg-teal-100 text-teal-950 ring-teal-300",
  User: "bg-stone-100 text-stone-700 ring-stone-300",
  Approved: "bg-emerald-100 text-emerald-950 ring-emerald-300",
  NeedsReview: "bg-amber-100 text-amber-950 ring-amber-300",
  Claim: "bg-stone-100 text-stone-700 ring-stone-300",
  ArticleText: "bg-stone-100 text-stone-700 ring-stone-300",
};

const labels: Record<string, string> = {
  Supported: "Supported",
  MostlySupported: "Mostly supported",
  Mixed: "Mixed",
  MostlyContradicted: "Mostly contradicted",
  Contradicted: "Contradicted",
  InsufficientEvidence: "Insufficient evidence",
  Pending: "Waiting to start",
  ExtractingClaims: "Extracting claims",
  GatheringEvidence: "Collecting sources",
  AnalyzingEvidence: "Classifying sources",
  GeneratingConclusion: "Calculating verdict",
  Completed: "Completed",
  Failed: "Could not finish",
  Claim: "Claim",
  ArticleText: "Article text",
  NeedsReview: "Needs review",
  Approved: "Approved",
};

export function labelFor(value: string): string {
  return labels[value] ?? value;
}

export function Pill({ value, children }: { value: string; children?: string }) {
  const tone = tones[value] ?? "bg-stone-100 text-stone-700 ring-stone-300";
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-1 font-sans text-xs font-medium ring-1 ring-inset ${tone}`}>
      {children ?? labelFor(value)}
    </span>
  );
}
