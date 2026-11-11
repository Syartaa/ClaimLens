namespace ClaimLens.Application.Admin;

public interface IAdminStore
{
    Task<AdminDashboard> DashboardAsync(CancellationToken cancellationToken);

    Task<(IReadOnlyList<AdminUserItem> Items, int TotalCount)> ListUsersAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<AdminInvestigationItem> Items, int TotalCount)> ListInvestigationsAsync(
        int page,
        int pageSize,
        string? processingStatus,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<AdminAiRunItem> Items, int TotalCount)> ListAiRunsAsync(
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken);
}
