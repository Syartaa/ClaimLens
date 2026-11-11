using ClaimLens.Application.Investigations;
using ClaimLens.Domain.Investigations;

namespace ClaimLens.Infrastructure.EvidenceCollection;

public sealed class MockEvidenceProvider : IEvidenceProvider
{
    public string Name => "mock";

    public Task<IReadOnlyList<RetrievedEvidence>> RetrieveAsync(string claimText, CancellationToken cancellationToken)
    {
        var excerpt = claimText.Trim();
        IReadOnlyList<RetrievedEvidence> items =
        [
            new(
                "https://public-health.example/note",
                "Fixture public-health note",
                "A. Fixture",
                "ClaimLens fixtures",
                new DateTimeOffset(2020, 3, 1, 0, 0, 0, TimeSpan.Zero),
                excerpt,
                new SourceQualityFactors(true, true, true, false, true, false)),
            new(
                "https://short-note.example/note",
                "Fixture short note",
                null,
                "ClaimLens fixtures",
                null,
                "A short fixture note that does not assess the claim.",
                new SourceQualityFactors(true, false, false, false, false, false))
        ];
        return Task.FromResult(items);
    }
}
