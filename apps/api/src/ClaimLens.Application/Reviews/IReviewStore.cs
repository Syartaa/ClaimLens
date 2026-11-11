using ClaimLens.Domain.Investigations;

namespace ClaimLens.Application.Reviews;

public interface IReviewStore
{
    Task<Evidence?> FindEvidenceAsync(Guid evidenceId, CancellationToken cancellationToken);

    Task<Investigation?> FindInvestigationAsync(Guid investigationId, CancellationToken cancellationToken);

    Task<Source?> FindSourceAsync(Guid sourceId, CancellationToken cancellationToken);

    Task<bool> SourceUsedByInvestigationAsync(Guid sourceId, Guid investigationId, CancellationToken cancellationToken);

    Task AddReviewAsync(EvidenceReview review, CancellationToken cancellationToken);

    Task AddAuditAsync(AuditEvent audit, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ReviewQueueItem> Items, int TotalCount)> ListCompletedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
