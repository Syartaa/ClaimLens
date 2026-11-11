using ClaimLens.Application.Investigations;
using Microsoft.AspNetCore.SignalR;

namespace ClaimLens.Api.Hubs;

public sealed class SignalRInvestigationProgress : IInvestigationProgress
{
    private readonly IHubContext<InvestigationsHub> _hub;

    public SignalRInvestigationProgress(IHubContext<InvestigationsHub> hub)
    {
        _hub = hub;
    }

    public Task PublishAsync(
        Guid investigationId,
        string processingStatus,
        string? verdict,
        string? failureCode,
        CancellationToken cancellationToken) =>
        _hub.Clients.Group(InvestigationsHub.GroupName(investigationId)).SendAsync(
            InvestigationsHub.StatusChanged,
            new { investigationId, processingStatus, verdict, failureCode },
            cancellationToken);
}
