using ClaimLens.Application.Investigations;
using ClaimLens.Application.Reviews;
using ClaimLens.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimLens.Api.Controllers;

[ApiController]
[Authorize(Roles = "Reviewer,Admin")]
[Route("api/reviews")]
public sealed class ReviewsController : ControllerBase
{
    private readonly ReviewService _reviews;

    public ReviewsController(ReviewService reviews)
    {
        _reviews = reviews;
    }

    [HttpGet("queue")]
    public async Task<ActionResult<PagedResponse<ReviewQueueItem>>> Queue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _reviews.QueueAsync(page, pageSize, cancellationToken));
    }

    [HttpPost("evidence/{id:guid}")]
    public async Task<ActionResult<InvestigationResponse>> ReviewEvidence(
        Guid id,
        ReviewEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _reviews.ReviewEvidenceAsync(CurrentUserId.From(User), id, request, cancellationToken);
        return Created($"/api/investigations/{updated.Id}", updated);
    }

    [HttpPost("sources/{id:guid}/flag")]
    public async Task<ActionResult<InvestigationResponse>> FlagSource(
        Guid id,
        FlagSourceRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _reviews.FlagSourceAsync(CurrentUserId.From(User), id, request, cancellationToken);
        return Created($"/api/investigations/{updated.Id}", updated);
    }

    [HttpPost("investigations/{id:guid}/approve")]
    public async Task<ActionResult<InvestigationResponse>> Approve(
        Guid id,
        ApproveInvestigationRequest? request,
        CancellationToken cancellationToken)
    {
        var updated = await _reviews.ApproveAsync(
            CurrentUserId.From(User),
            id,
            request ?? new ApproveInvestigationRequest(null),
            cancellationToken);
        return Ok(updated);
    }
}
