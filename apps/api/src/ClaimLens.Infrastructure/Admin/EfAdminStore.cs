using ClaimLens.Application.Admin;
using ClaimLens.Domain.Investigations;
using ClaimLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClaimLens.Infrastructure.Admin;

public sealed class EfAdminStore : IAdminStore
{
    private readonly ClaimLensDbContext _db;

    public EfAdminStore(ClaimLensDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboard> DashboardAsync(CancellationToken cancellationToken)
    {
        var users = await _db.Users.CountAsync(cancellationToken);
        var activeUsers = await _db.Users.CountAsync(user => user.IsActive, cancellationToken);
        var investigations = await _db.Investigations.CountAsync(cancellationToken);
        var completed = await _db.Investigations.CountAsync(
            investigation => investigation.ProcessingStatus == ProcessingStatus.Completed,
            cancellationToken);
        var failed = await _db.Investigations.CountAsync(
            investigation => investigation.ProcessingStatus == ProcessingStatus.Failed,
            cancellationToken);
        var aiRuns = await _db.AiRuns.CountAsync(cancellationToken);
        var aiRunFailures = await _db.AiRuns.CountAsync(run => run.Status == AiRunStatus.Failed, cancellationToken);

        var failureRows = await _db.Investigations
            .Where(investigation => investigation.ProcessingStatus == ProcessingStatus.Failed && investigation.FailureCode != null)
            .GroupBy(investigation => investigation.FailureCode!)
            .Select(group => new { Label = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var providerRows = await _db.Investigations
            .Where(investigation => investigation.EvidenceProvider != null)
            .GroupBy(investigation => investigation.EvidenceProvider!)
            .Select(group => new { Label = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var modelRows = await _db.AiRuns
            .GroupBy(run => run.ModelUsed)
            .Select(group => new { Label = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var failureCodes = failureRows.Select(row => new CountByLabel(row.Label, row.Count));
        var providers = providerRows.Select(row => new CountByLabel(row.Label, row.Count));
        var models = modelRows.Select(row => new CountByLabel(row.Label, row.Count));

        return new AdminDashboard(
            users,
            activeUsers,
            investigations,
            investigations - completed - failed,
            completed,
            failed,
            aiRuns,
            aiRunFailures,
            failureCodes.OrderByDescending(item => item.Count).ToArray(),
            providers.OrderByDescending(item => item.Count).ToArray(),
            models.OrderByDescending(item => item.Count).ToArray());
    }

    public async Task<(IReadOnlyList<AdminUserItem> Items, int TotalCount)> ListUsersAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var users = _db.Users;
        var total = await users.CountAsync(cancellationToken);
        var rows = await users
            .OrderByDescending(user => user.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role,
                user.IsActive,
                user.CreatedAt
            })
            .ToListAsync(cancellationToken);
        var items = rows
            .Select(user => new AdminUserItem(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role.ToString(),
                user.IsActive,
                user.CreatedAt))
            .ToArray();
        return (items, total);
    }

    public async Task<(IReadOnlyList<AdminInvestigationItem> Items, int TotalCount)> ListInvestigationsAsync(
        int page,
        int pageSize,
        string? processingStatus,
        CancellationToken cancellationToken)
    {
        var investigations = _db.Investigations.AsQueryable();
        if (Enum.TryParse<ProcessingStatus>(processingStatus, out var status))
        {
            investigations = investigations.Where(investigation => investigation.ProcessingStatus == status);
        }

        var total = await investigations.CountAsync(cancellationToken);
        var rows = await investigations
            .OrderByDescending(investigation => investigation.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(investigation => new
            {
                investigation.Id,
                investigation.Title,
                investigation.User.Email,
                investigation.ProcessingStatus,
                investigation.Verdict,
                investigation.FailureCode,
                investigation.EvidenceProvider,
                investigation.CreatedAt,
                investigation.CompletedAt
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new AdminInvestigationItem(
                row.Id,
                row.Title,
                row.Email,
                row.ProcessingStatus.ToString(),
                row.Verdict?.ToString(),
                row.FailureCode,
                row.EvidenceProvider,
                row.CreatedAt,
                row.CompletedAt))
            .ToArray();
        return (items, total);
    }

    public async Task<(IReadOnlyList<AdminAiRunItem> Items, int TotalCount)> ListAiRunsAsync(
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken)
    {
        var runs = _db.AiRuns.AsQueryable();
        if (status == "Failed")
        {
            runs = runs.Where(run => run.Status == AiRunStatus.Failed);
        }
        else if (status == "Succeeded")
        {
            runs = runs.Where(run => run.Status == AiRunStatus.Succeeded);
        }

        var total = await runs.CountAsync(cancellationToken);
        var rows = await runs
            .OrderByDescending(run => run.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(run => new
            {
                run.Id,
                run.InvestigationId,
                run.Purpose,
                run.PromptName,
                run.PromptVersion,
                run.ModelUsed,
                run.Status,
                run.ErrorCategory,
                run.LatencyMs,
                run.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new AdminAiRunItem(
                row.Id,
                row.InvestigationId,
                row.Purpose.ToString(),
                row.PromptName,
                row.PromptVersion,
                row.ModelUsed,
                row.Status.ToString(),
                row.ErrorCategory,
                row.LatencyMs,
                row.CreatedAt))
            .ToArray();
        return (items, total);
    }
}
