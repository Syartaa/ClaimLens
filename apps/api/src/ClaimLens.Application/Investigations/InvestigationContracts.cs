namespace ClaimLens.Application.Investigations;

public sealed record CreateInvestigationRequest(string InputType, string? Title, string Text);

public sealed record InvestigationListQuery(
    int Page,
    int PageSize,
    string? Search,
    string? Verdict,
    string? ProcessingStatus,
    string? InputType,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string Sort);

public sealed record EvidenceResponse(
    Guid Id,
    string SourceTitle,
    string Domain,
    string Url,
    DateTimeOffset? PublishedAt,
    string Excerpt,
    string? QualityBand,
    string? AiStance,
    string? Stance,
    decimal? RelevanceScore,
    decimal? ConfidenceScore,
    string? Explanation,
    string? Publisher,
    bool SourceFlagged,
    string? FlagNote,
    string? ReviewNote,
    Guid SourceId);

public sealed record ClaimResponse(
    Guid Id,
    int Position,
    string ClaimText,
    string NormalizedClaim,
    string ClaimType,
    decimal ImportanceScore,
    IReadOnlyList<EvidenceResponse> Evidence);

public sealed record InvestigationResponse(
    Guid Id,
    string Title,
    string InputType,
    string ProcessingStatus,
    string? Verdict,
    decimal? ConfidenceScore,
    string? Summary,
    string? FailureCode,
    string OriginalInput,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    string? EvidenceProvider,
    IReadOnlyList<ClaimResponse> Claims,
    IReadOnlyList<TimelineEvent> Timeline,
    Guid OwnerId,
    DateTimeOffset? ApprovedAt);

public sealed record TimelineEvent(string Type, string Message, DateTimeOffset At);

public sealed record VerdictCount(string Verdict, int Count);

public sealed record InvestigationSummary(
    int Total,
    int Completed,
    int InProgress,
    int Failed,
    int ClaimsAnalyzed,
    IReadOnlyList<VerdictCount> Verdicts);

public sealed record InvestigationListItem(
    Guid Id,
    string Title,
    string InputType,
    string ProcessingStatus,
    string? Verdict,
    decimal? ConfidenceScore,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
