using ClaimLens.Domain.Users;

namespace ClaimLens.Domain.Investigations;

public sealed class AuditEvent
{
    public Guid Id { get; set; }

    public Guid InvestigationId { get; set; }

    public Investigation Investigation { get; set; } = null!;

    public Guid ActorUserId { get; set; }

    public User Actor { get; set; } = null!;

    public string EventType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Metadata { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }
}
