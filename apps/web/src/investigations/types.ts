export type Investigation = {
  id: string;
  title: string;
  inputType: string;
  processingStatus: string;
  verdict: string | null;
  confidenceScore: number | null;
  summary: string | null;
  failureCode: string | null;
  originalInput: string;
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
  evidenceProvider: string | null;
  claims: ExtractedClaim[];
  timeline: TimelineEvent[];
  ownerId: string;
  approvedAt: string | null;
};

export type TimelineEvent = {
  type: string;
  message: string;
  at: string;
};

export type InvestigationSummary = {
  total: number;
  completed: number;
  inProgress: number;
  failed: number;
  claimsAnalyzed: number;
  verdicts: { verdict: string; count: number }[];
};

export type ExtractedClaim = {
  id: string;
  position: number;
  claimText: string;
  normalizedClaim: string;
  claimType: string;
  importanceScore: number;
  evidence: RetrievedEvidence[];
};

export type RetrievedEvidence = {
  id: string;
  sourceTitle: string;
  domain: string;
  url: string;
  publishedAt: string | null;
  excerpt: string;
  qualityBand: string | null;
  aiStance: string | null;
  stance: string | null;
  relevanceScore: number | null;
  confidenceScore: number | null;
  explanation: string | null;
  publisher: string | null;
  sourceFlagged: boolean;
  flagNote: string | null;
  reviewNote: string | null;
  sourceId: string;
};

export type InvestigationListItem = {
  id: string;
  title: string;
  inputType: string;
  processingStatus: string;
  verdict: string | null;
  confidenceScore: number | null;
  createdAt: string;
  completedAt: string | null;
};

export type InvestigationList = {
  items: InvestigationListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
};
