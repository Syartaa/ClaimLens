namespace ClaimLens.Application.Admin;

public sealed record CountByLabel(string Label, int Count);

public sealed record AdminDashboard(
    int Users,
    int ActiveUsers,
    int Investigations,
    int InProgress,
    int Completed,
    int Failed,
    int AiRuns,
    int AiRunFailures,
    IReadOnlyList<CountByLabel> FailureCodes,
    IReadOnlyList<CountByLabel> Providers,
    IReadOnlyList<CountByLabel> Models);

public sealed record AdminUserItem(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record AdminInvestigationItem(
    Guid Id,
    string Title,
    string OwnerEmail,
    string ProcessingStatus,
    string? Verdict,
    string? FailureCode,
    string? EvidenceProvider,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record AdminAiRunItem(
    Guid Id,
    Guid InvestigationId,
    string Purpose,
    string PromptName,
    string PromptVersion,
    string ModelUsed,
    string Status,
    string? ErrorCategory,
    int? LatencyMs,
    DateTimeOffset CreatedAt);
