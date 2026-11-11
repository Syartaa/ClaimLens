using ClaimLens.Domain.Users;

namespace ClaimLens.Domain.Investigations;

public sealed class Investigation
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string OriginalInput { get; set; } = string.Empty;

    public InputType InputType { get; set; }

    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Pending;

    public Verdict? Verdict { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public string? Summary { get; set; }

    public string? FailureCode { get; set; }

    public string? EvidenceProvider { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }

    public Guid? ApprovedByUserId { get; set; }
}
