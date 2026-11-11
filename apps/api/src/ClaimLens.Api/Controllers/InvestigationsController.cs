using ClaimLens.Api.Auth;
using ClaimLens.Application.Investigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClaimLens.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/investigations")]
public sealed class InvestigationsController : ControllerBase
{
    private readonly InvestigationService _investigations;

    public InvestigationsController(InvestigationService investigations)
    {
        _investigations = investigations;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<InvestigationListItem>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? q = null,
        [FromQuery] string? verdict = null,
        [FromQuery] string? processingStatus = null,
        [FromQuery] string? inputType = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string sort = "-createdAt",
        CancellationToken cancellationToken = default)
    {
        var query = new InvestigationListQuery(page, pageSize, q, verdict, processingStatus, inputType, from, to, sort);
        return Ok(await _investigations.ListAsync(CurrentUserId.From(User), query, cancellationToken));
    }

    [EnableRateLimiting("create-investigation")]
    [HttpPost]
    public async Task<ActionResult<InvestigationResponse>> Create(
        CreateInvestigationRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _investigations.CreateAsync(CurrentUserId.From(User), request, cancellationToken);
        return Accepted($"/api/investigations/{created.Id}", created);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<InvestigationSummary>> Summary(CancellationToken cancellationToken)
    {
        return Ok(await _investigations.SummarizeAsync(CurrentUserId.From(User), cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<InvestigationResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _investigations.CancelAsync(CurrentUserId.From(User), id, cancellationToken));
    }

    [HttpPost("{id:guid}/reanalyze")]
    public async Task<ActionResult<InvestigationResponse>> Reanalyze(Guid id, CancellationToken cancellationToken)
    {
        var restarted = User.IsInRole("Admin")
            ? await _investigations.ReanalyzeForAdminAsync(id, cancellationToken)
            : await _investigations.ReanalyzeAsync(CurrentUserId.From(User), id, cancellationToken);
        return Accepted($"/api/investigations/{restarted.Id}", restarted);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvestigationResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Reviewer") || User.IsInRole("Admin"))
        {
            return Ok(await _investigations.GetForStaffAsync(id, cancellationToken));
        }

        return Ok(await _investigations.GetAsync(CurrentUserId.From(User), id, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            await _investigations.DeleteForAdminAsync(id, cancellationToken);
        }
        else
        {
            await _investigations.DeleteAsync(CurrentUserId.From(User), id, cancellationToken);
        }
        return NoContent();
    }
}
