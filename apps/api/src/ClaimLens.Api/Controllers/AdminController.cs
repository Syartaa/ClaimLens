using ClaimLens.Application.Admin;
using ClaimLens.Application.Investigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimLens.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly AdminService _admin;

    public AdminController(AdminService admin)
    {
        _admin = admin;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboard>> Dashboard(CancellationToken cancellationToken)
    {
        return Ok(await _admin.DashboardAsync(cancellationToken));
    }

    [HttpGet("users")]
    public async Task<ActionResult<PagedResponse<AdminUserItem>>> Users(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _admin.UsersAsync(page, pageSize, cancellationToken));
    }

    [HttpGet("investigations")]
    public async Task<ActionResult<PagedResponse<AdminInvestigationItem>>> Investigations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? processingStatus = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _admin.InvestigationsAsync(page, pageSize, processingStatus, cancellationToken));
    }

    [HttpGet("ai-runs")]
    public async Task<ActionResult<PagedResponse<AdminAiRunItem>>> AiRuns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _admin.AiRunsAsync(page, pageSize, status, cancellationToken));
    }
}
