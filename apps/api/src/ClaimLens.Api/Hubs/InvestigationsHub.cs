using ClaimLens.Api.Auth;
using ClaimLens.Application.Investigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ClaimLens.Api.Hubs;

[Authorize]
public sealed class InvestigationsHub : Hub
{
    public const string StatusChanged = "statusChanged";

    private readonly IInvestigationStore _store;

    public InvestigationsHub(IInvestigationStore store)
    {
        _store = store;
    }

    public async Task Join(Guid investigationId)
    {
        var caller = Context.User!;
        var investigation = caller.IsInRole("Reviewer") || caller.IsInRole("Admin")
            ? await _store.FindByIdAsync(investigationId, Context.ConnectionAborted)
            : await _store.FindForUserAsync(investigationId, CurrentUserId.From(caller), Context.ConnectionAborted);
        if (investigation is null)
        {
            throw new HubException("Investigation could not be found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(investigationId));
    }

    public static string GroupName(Guid investigationId) => $"investigation:{investigationId}";
}
