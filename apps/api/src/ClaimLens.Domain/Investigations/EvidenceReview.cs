using ClaimLens.Domain.Users;

namespace ClaimLens.Domain.Investigations;

public sealed class EvidenceReview
{
    public Guid Id { get; set; }

    public Guid EvidenceId { get; set; }

    public Evidence Evidence { get; set; } = null!;

    public Guid ReviewerId { get; set; }

    public User Reviewer { get; set; } = null!;

    public Stance PreviousStance { get; set; }

    public Stance NewStance { get; set; }

    public string Note { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
