using ClaimLens.Application.Investigations;
using ClaimLens.Domain.Investigations;
using ClaimLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClaimLens.Infrastructure.Investigations;

public sealed class EfInvestigationStore : IInvestigationStore
{
    private readonly ClaimLensDbContext _db;
    private readonly IInvestigationProgress _progress;

    public EfInvestigationStore(ClaimLensDbContext db, IInvestigationProgress progress)
    {
        _db = db;
        _progress = progress;
    }

    public Task AddAsync(Investigation investigation, CancellationToken cancellationToken)
    {
        _db.Investigations.Add(investigation);
        return Task.CompletedTask;
    }

    public async Task<Investigation?> ClaimNextPendingAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var next = await _db.Investigations
            .FromSqlRaw("""
                SELECT * FROM investigations
                WHERE "ProcessingStatus" = 'Pending'
                ORDER BY "CreatedAt"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .AsTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (next is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var claimedAt = DateTimeOffset.UtcNow;
        if (next.AttemptCount >= WorkerLimits.MaxAttempts)
        {
            next.ProcessingStatus = ProcessingStatus.Failed;
            next.FailureCode = WorkerLimits.InterruptedCode;
            next.UpdatedAt = claimedAt;
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await PublishAsync([next], cancellationToken);
            return null;
        }

        next.AttemptCount += 1;
        next.ProcessingStatus = ProcessingStatus.ExtractingClaims;
        next.UpdatedAt = claimedAt;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishAsync([next], cancellationToken);
        return next;
    }

    public async Task<Investigation?> ClaimNextGatheringAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var next = await _db.Investigations
            .FromSqlRaw("""
                SELECT * FROM investigations
                WHERE "ProcessingStatus" = 'GatheringEvidence'
                ORDER BY "CreatedAt"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .AsTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (next is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        next.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishAsync([next], cancellationToken);
        return next;
    }

    public async Task<Investigation?> ClaimNextAnalyzingAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var next = await _db.Investigations
            .FromSqlRaw("""
                SELECT * FROM investigations
                WHERE "ProcessingStatus" = 'AnalyzingEvidence'
                ORDER BY "CreatedAt"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .AsTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (next is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        next.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishAsync([next], cancellationToken);
        return next;
    }

    public async Task<Investigation?> ClaimNextConcludingAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var next = await _db.Investigations
            .FromSqlRaw("""
                SELECT * FROM investigations
                WHERE "ProcessingStatus" = 'GeneratingConclusion'
                ORDER BY "CreatedAt"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .AsTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (next is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        next.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishAsync([next], cancellationToken);
        return next;
    }

    public Task AddAnalysisResultsAsync(IReadOnlyList<AnalysisResult> results, CancellationToken cancellationToken)
    {
        _db.AnalysisResults.AddRange(results);
        return Task.CompletedTask;
    }

    public Task<Source?> FindSourceByUrlAsync(string url, CancellationToken cancellationToken) =>
        _db.Sources.Include(source => source.Quality)
            .SingleOrDefaultAsync(source => source.Url == url, cancellationToken);

    public Task AddSourceAsync(Source source, CancellationToken cancellationToken)
    {
        _db.Sources.Add(source);
        return Task.CompletedTask;
    }

    public Task AddEvidenceAsync(IReadOnlyList<Evidence> evidence, CancellationToken cancellationToken)
    {
        _db.Evidence.AddRange(evidence);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Evidence>> ListEvidenceForClaimsAsync(IReadOnlyList<Guid> claimIds, CancellationToken cancellationToken)
    {
        if (claimIds.Count == 0)
        {
            return [];
        }

        return await _db.Evidence
            .Include(item => item.Source)
            .ThenInclude(source => source.Quality)
            .Include(item => item.Reviews)
            .Where(item => claimIds.Contains(item.ClaimId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Claim>> ListClaimsAsync(Guid investigationId, CancellationToken cancellationToken) =>
        await _db.Claims.Where(claim => claim.InvestigationId == investigationId)
            .OrderBy(claim => claim.Position)
            .ToListAsync(cancellationToken);

    public Task AddClaimsAsync(IReadOnlyList<Claim> claims, CancellationToken cancellationToken)
    {
        _db.Claims.AddRange(claims);
        return Task.CompletedTask;
    }

    public Task AddAiRunAsync(AiRun run, CancellationToken cancellationToken)
    {
        _db.AiRuns.Add(run);
        return Task.CompletedTask;
    }

    public Task<Investigation?> FindForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        _db.Investigations.SingleOrDefaultAsync(
            investigation => investigation.Id == id && investigation.UserId == userId,
            cancellationToken);

    public Task<Investigation?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Investigations.SingleOrDefaultAsync(investigation => investigation.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AuditEvent>> ListAuditsAsync(Guid investigationId, CancellationToken cancellationToken) =>
        await _db.AuditEvents
            .Where(audit => audit.InvestigationId == investigationId)
            .OrderBy(audit => audit.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Investigation> Items, int TotalCount)> ListForUserAsync(
        Guid userId,
        InvestigationListQuery query,
        CancellationToken cancellationToken)
    {
        var investigations = _db.Investigations.Where(investigation => investigation.UserId == userId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            investigations = investigations.Where(investigation =>
                EF.Functions.ILike(investigation.Title, $"%{search}%")
                || EF.Functions.ILike(investigation.OriginalInput, $"%{search}%"));
        }

        if (Enum.TryParse<ProcessingStatus>(query.ProcessingStatus, out var status))
        {
            investigations = investigations.Where(investigation => investigation.ProcessingStatus == status);
        }

        if (Enum.TryParse<Verdict>(query.Verdict, out var verdict))
        {
            investigations = investigations.Where(investigation => investigation.Verdict == verdict);
        }

        if (Enum.TryParse<InputType>(query.InputType, out var inputType) && inputType != InputType.Url)
        {
            investigations = investigations.Where(investigation => investigation.InputType == inputType);
        }

        if (query.From is not null)
        {
            investigations = investigations.Where(investigation => investigation.CreatedAt >= query.From);
        }

        if (query.To is not null)
        {
            investigations = investigations.Where(investigation => investigation.CreatedAt <= query.To);
        }

        var total = await investigations.CountAsync(cancellationToken);
        var descending = query.Sort.StartsWith('-');
        var byUpdated = query.Sort.Contains("updatedAt", StringComparison.OrdinalIgnoreCase);
        investigations = (byUpdated, descending) switch
        {
            (true, true) => investigations.OrderByDescending(investigation => investigation.UpdatedAt),
            (true, false) => investigations.OrderBy(investigation => investigation.UpdatedAt),
            (false, false) => investigations.OrderBy(investigation => investigation.CreatedAt),
            _ => investigations.OrderByDescending(investigation => investigation.CreatedAt)
        };

        var items = await investigations
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Remove(Investigation investigation) => _db.Investigations.Remove(investigation);

    public async Task RemoveClaimsAsync(Guid investigationId, CancellationToken cancellationToken)
    {
        var claims = await _db.Claims.Where(claim => claim.InvestigationId == investigationId).ToListAsync(cancellationToken);
        _db.Claims.RemoveRange(claims);
    }

    public async Task<InvestigationSummary> SummarizeForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var investigations = _db.Investigations.Where(investigation => investigation.UserId == userId);
        var total = await investigations.CountAsync(cancellationToken);
        var completed = await investigations.CountAsync(investigation => investigation.ProcessingStatus == ProcessingStatus.Completed, cancellationToken);
        var failed = await investigations.CountAsync(investigation => investigation.ProcessingStatus == ProcessingStatus.Failed, cancellationToken);
        var claimsAnalyzed = await _db.AnalysisResults.CountAsync(
            result => result.Claim.Investigation.UserId == userId,
            cancellationToken);
        var verdictValues = await investigations
            .Where(investigation => investigation.ProcessingStatus == ProcessingStatus.Completed && investigation.Verdict != null)
            .Select(investigation => investigation.Verdict)
            .ToListAsync(cancellationToken);
        var verdicts = verdictValues
            .GroupBy(verdict => verdict!.Value)
            .Select(group => new VerdictCount(group.Key.ToString(), group.Count()))
            .ToArray();

        return new InvestigationSummary(total, completed, total - completed - failed, failed, claimsAnalyzed, verdicts);
    }

    public async Task<bool> StillOwnsAsync(Guid id, ProcessingStatus expected, CancellationToken cancellationToken)
    {
        var status = await _db.Investigations.AsNoTracking()
            .Where(investigation => investigation.Id == id)
            .Select(investigation => investigation.ProcessingStatus)
            .SingleAsync(cancellationToken);
        return status == expected;
    }

    public async Task<int> RecoverStaleAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = new[]
        {
            ProcessingStatus.ExtractingClaims,
            ProcessingStatus.GatheringEvidence,
            ProcessingStatus.AnalyzingEvidence,
            ProcessingStatus.GeneratingConclusion
        };
        var candidates = await _db.Investigations
            .Where(investigation => active.Contains(investigation.ProcessingStatus))
            .ToListAsync(cancellationToken);
        var recovered = 0;
        foreach (var investigation in candidates)
        {
            var action = WorkerRecovery.Decide(investigation.ProcessingStatus, investigation.UpdatedAt, investigation.AttemptCount, now);
            if (action == RecoveryAction.None)
            {
                continue;
            }

            if (action == RecoveryAction.Fail)
            {
                investigation.ProcessingStatus = ProcessingStatus.Failed;
                investigation.FailureCode = WorkerLimits.InterruptedCode;
            }
            else
            {
                var claims = await _db.Claims.Where(claim => claim.InvestigationId == investigation.Id).ToListAsync(cancellationToken);
                _db.Claims.RemoveRange(claims);
                investigation.ProcessingStatus = ProcessingStatus.Pending;
                investigation.Verdict = null;
                investigation.ConfidenceScore = null;
                investigation.Summary = null;
                investigation.FailureCode = null;
                investigation.EvidenceProvider = null;
                investigation.CompletedAt = null;
                investigation.ApprovedAt = null;
                investigation.ApprovedByUserId = null;
            }

            investigation.UpdatedAt = now;
            recovered++;
        }

        if (recovered > 0)
        {
            var notices = candidates.Where(investigation => investigation.UpdatedAt == now).ToArray();
            await _db.SaveChangesAsync(cancellationToken);
            await PublishAsync(notices, cancellationToken);
        }

        return recovered;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var notices = ChangedInvestigations();
        await _db.SaveChangesAsync(cancellationToken);
        await PublishAsync(notices, cancellationToken);
    }

    private Investigation[] ChangedInvestigations() =>
        _db.ChangeTracker.Entries<Investigation>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Where(entry => entry.State == EntityState.Added
                || entry.Property(investigation => investigation.ProcessingStatus).IsModified
                || entry.Property(investigation => investigation.Verdict).IsModified
                || entry.Property(investigation => investigation.FailureCode).IsModified)
            .Select(entry => entry.Entity)
            .ToArray();

    private async Task PublishAsync(IEnumerable<Investigation> investigations, CancellationToken cancellationToken)
    {
        foreach (var investigation in investigations)
        {
            await _progress.PublishAsync(
                investigation.Id,
                investigation.ProcessingStatus.ToString(),
                investigation.Verdict?.ToString(),
                investigation.FailureCode,
                cancellationToken);
        }
    }
}
