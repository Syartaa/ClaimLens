namespace ClaimLens.Domain.Investigations;

public enum AiRunPurpose
{
    ClaimExtraction,
    EvidenceClassification,
    Summary
}

public enum AiRunStatus
{
    Succeeded,
    Failed
}

public sealed class AiRun
{
    public Guid Id { get; set; }

    public Guid InvestigationId { get; set; }

    public Guid? ClaimId { get; set; }

    public AiRunPurpose Purpose { get; set; }

    public string PromptName { get; set; } = string.Empty;

    public string PromptVersion { get; set; } = string.Empty;

    public string ModelUsed { get; set; } = string.Empty;

    public AiRunStatus Status { get; set; }

    public string? ErrorCategory { get; set; }

    public int? LatencyMs { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
