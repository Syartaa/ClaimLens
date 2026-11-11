namespace ClaimLens.Domain.Investigations;

public sealed class Evidence
{
    public Guid Id { get; set; }

    public Guid ClaimId { get; set; }

    public Claim Claim { get; set; } = null!;

    public Guid SourceId { get; set; }

    public Source Source { get; set; } = null!;

    public string EvidenceText { get; set; } = string.Empty;

    public Stance? AiStance { get; set; }

    public Stance? Stance { get; set; }

    public decimal? RelevanceScore { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public string? Explanation { get; set; }

    public string? PromptVersion { get; set; }

    public string? ModelUsed { get; set; }

    public List<EvidenceReview> Reviews { get; set; } = [];
}
