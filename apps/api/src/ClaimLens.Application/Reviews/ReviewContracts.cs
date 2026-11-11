namespace ClaimLens.Application.Reviews;

public sealed record ReviewEvidenceRequest(string Stance, string Note);

public sealed record FlagSourceRequest(Guid InvestigationId, string Note);

public sealed record ApproveInvestigationRequest(string? Note);

public sealed record ReviewQueueItem(
    Guid Id,
    string Title,
    string? Verdict,
    decimal? ConfidenceScore,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? ApprovedAt,
    string OwnerEmail);
