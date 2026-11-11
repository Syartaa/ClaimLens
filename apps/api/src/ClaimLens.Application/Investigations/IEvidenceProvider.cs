using ClaimLens.Domain.Investigations;

namespace ClaimLens.Application.Investigations;

public interface IEvidenceProvider
{
    string Name { get; }

    Task<IReadOnlyList<RetrievedEvidence>> RetrieveAsync(string claimText, CancellationToken cancellationToken);
}

public sealed record RetrievedEvidence(
    string Url,
    string Title,
    string? Author,
    string? Publisher,
    DateTimeOffset? PublishedAt,
    string Excerpt,
    SourceQualityFactors Factors);
