namespace ClaimLens.Application.Investigations;

public interface IInvestigationProgress
{
    Task PublishAsync(
        Guid investigationId,
        string processingStatus,
        string? verdict,
        string? failureCode,
        CancellationToken cancellationToken);
}

public sealed class NoInvestigationProgress : IInvestigationProgress
{
    public Task PublishAsync(
        Guid investigationId,
        string processingStatus,
        string? verdict,
        string? failureCode,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
