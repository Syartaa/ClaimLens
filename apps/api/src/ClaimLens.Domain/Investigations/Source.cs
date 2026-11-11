namespace ClaimLens.Domain.Investigations;

public sealed class Source
{
    public Guid Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Author { get; set; }

    public string? Publisher { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset RetrievedAt { get; set; }

    public DateTimeOffset? FlaggedAt { get; set; }

    public string? FlagNote { get; set; }

    public Guid? FlaggedByUserId { get; set; }

    public SourceQuality? Quality { get; set; }
}
