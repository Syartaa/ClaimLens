using ClaimLens.Domain.Investigations;

namespace ClaimLens.Application.Investigations;

public interface IInvestigationStore
{
    Task AddAsync(Investigation investigation, CancellationToken cancellationToken);

    Task<Investigation?> ClaimNextPendingAsync(CancellationToken cancellationToken);

    Task<Investigation?> ClaimNextGatheringAsync(CancellationToken cancellationToken);

    Task<Investigation?> ClaimNextAnalyzingAsync(CancellationToken cancellationToken);

    Task<Investigation?> ClaimNextConcludingAsync(CancellationToken cancellationToken);

    Task AddAnalysisResultsAsync(IReadOnlyList<AnalysisResult> results, CancellationToken cancellationToken);

    Task<Source?> FindSourceByUrlAsync(string url, CancellationToken cancellationToken);

    Task AddSourceAsync(Source source, CancellationToken cancellationToken);

    Task AddEvidenceAsync(IReadOnlyList<Evidence> evidence, CancellationToken cancellationToken);

    Task<IReadOnlyList<Evidence>> ListEvidenceForClaimsAsync(IReadOnlyList<Guid> claimIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<Claim>> ListClaimsAsync(Guid investigationId, CancellationToken cancellationToken);

    Task AddClaimsAsync(IReadOnlyList<Claim> claims, CancellationToken cancellationToken);

    Task AddAiRunAsync(AiRun run, CancellationToken cancellationToken);

    Task<Investigation?> FindForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<Investigation?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditEvent>> ListAuditsAsync(Guid investigationId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Investigation> Items, int TotalCount)> ListForUserAsync(
        Guid userId,
        InvestigationListQuery query,
        CancellationToken cancellationToken);

    void Remove(Investigation investigation);

    Task RemoveClaimsAsync(Guid investigationId, CancellationToken cancellationToken);

    Task<InvestigationSummary> SummarizeForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> StillOwnsAsync(Guid id, ProcessingStatus expected, CancellationToken cancellationToken);

    Task<int> RecoverStaleAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
