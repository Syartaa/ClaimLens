namespace ClaimLens.Domain.Investigations;

public sealed class Claim
{
    public Guid Id { get; set; }

    public Guid InvestigationId { get; set; }

    public Investigation Investigation { get; set; } = null!;

    public int Position { get; set; }

    public string ClaimText { get; set; } = string.Empty;

    public string NormalizedClaim { get; set; } = string.Empty;

    public ClaimType ClaimType { get; set; }

    public decimal ImportanceScore { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
