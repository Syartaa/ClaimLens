using ClaimLens.Application.Common;
using ClaimLens.Application.Investigations;
using ClaimLens.Domain.Investigations;

namespace ClaimLens.Application.Admin;

public sealed class AdminService
{
    private readonly IAdminStore _store;

    public AdminService(IAdminStore store)
    {
        _store = store;
    }

    public Task<AdminDashboard> DashboardAsync(CancellationToken cancellationToken) =>
        _store.DashboardAsync(cancellationToken);

    public async Task<PagedResponse<AdminUserItem>> UsersAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var (items, total) = await _store.ListUsersAsync(page, pageSize, cancellationToken);
        return new PagedResponse<AdminUserItem>(items, page, pageSize, total);
    }

    public async Task<PagedResponse<AdminInvestigationItem>> InvestigationsAsync(
        int page,
        int pageSize,
        string? processingStatus,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        if (!string.IsNullOrWhiteSpace(processingStatus) && !Enum.TryParse<ProcessingStatus>(processingStatus, out _))
        {
            throw new AppException("VALIDATION_FAILED", 400, "Processing status is not supported.");
        }

        var (items, total) = await _store.ListInvestigationsAsync(page, pageSize, processingStatus, cancellationToken);
        return new PagedResponse<AdminInvestigationItem>(items, page, pageSize, total);
    }

    public async Task<PagedResponse<AdminAiRunItem>> AiRunsAsync(
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        if (!string.IsNullOrWhiteSpace(status) && status is not ("Succeeded" or "Failed"))
        {
            throw new AppException("VALIDATION_FAILED", 400, "AI run status is not supported.");
        }

        var (items, total) = await _store.ListAiRunsAsync(page, pageSize, status, cancellationToken);
        return new PagedResponse<AdminAiRunItem>(items, page, pageSize, total);
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new AppException("VALIDATION_FAILED", 400, "Page must be at least 1.");
        }

        if (pageSize is < 1 or > 50)
        {
            throw new AppException("VALIDATION_FAILED", 400, "Page size must be between 1 and 50.");
        }
    }
}
