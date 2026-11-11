namespace ClaimLens.Domain.Investigations;

public sealed class AnalysisResult
{
    public Guid Id { get; set; }

    public Guid ClaimId { get; set; }

    public Claim Claim { get; set; } = null!;

    public Verdict Verdict { get; set; }

    public decimal Confidence { get; set; }

    public string Explanation { get; set; } = string.Empty;

    public string AlgorithmVersion { get; set; } = string.Empty;

    public DateTimeOffset GeneratedAt { get; set; }
}
