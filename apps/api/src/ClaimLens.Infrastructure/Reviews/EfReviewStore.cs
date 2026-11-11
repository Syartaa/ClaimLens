using ClaimLens.Application.Reviews;
using ClaimLens.Domain.Investigations;
using ClaimLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClaimLens.Infrastructure.Reviews;

public sealed class EfReviewStore : IReviewStore
{
    private readonly ClaimLensDbContext _db;

    public EfReviewStore(ClaimLensDbContext db)
    {
        _db = db;
    }

    public Task<Evidence?> FindEvidenceAsync(Guid evidenceId, CancellationToken cancellationToken) =>
        _db.Evidence.Include(item => item.Claim)
            .SingleOrDefaultAsync(item => item.Id == evidenceId, cancellationToken);

    public Task<Investigation?> FindInvestigationAsync(Guid investigationId, CancellationToken cancellationToken) =>
        _db.Investigations.SingleOrDefaultAsync(investigation => investigation.Id == investigationId, cancellationToken);

    public Task<Source?> FindSourceAsync(Guid sourceId, CancellationToken cancellationToken) =>
        _db.Sources.SingleOrDefaultAsync(source => source.Id == sourceId, cancellationToken);

    public Task<bool> SourceUsedByInvestigationAsync(Guid sourceId, Guid investigationId, CancellationToken cancellationToken) =>
        _db.Evidence.AnyAsync(
            item => item.SourceId == sourceId && item.Claim.InvestigationId == investigationId,
            cancellationToken);

    public Task AddReviewAsync(EvidenceReview review, CancellationToken cancellationToken)
    {
        _db.EvidenceReviews.Add(review);
        return Task.CompletedTask;
    }

    public Task AddAuditAsync(AuditEvent audit, CancellationToken cancellationToken)
    {
        _db.AuditEvents.Add(audit);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<ReviewQueueItem> Items, int TotalCount)> ListCompletedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var completed = _db.Investigations.Where(investigation => investigation.ProcessingStatus == ProcessingStatus.Completed);
        var total = await completed.CountAsync(cancellationToken);
        var rows = await completed
            .OrderBy(investigation => investigation.ApprovedAt == null ? 0 : 1)
            .ThenByDescending(investigation => investigation.CompletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(investigation => new
            {
                investigation.Id,
                investigation.Title,
                investigation.Verdict,
                investigation.ConfidenceScore,
                investigation.CompletedAt,
                investigation.ApprovedAt,
                investigation.User.Email
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new ReviewQueueItem(
                row.Id,
                row.Title,
                row.Verdict?.ToString(),
                row.ConfidenceScore,
                row.CompletedAt,
                row.ApprovedAt,
                row.Email))
            .ToArray();
        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}
