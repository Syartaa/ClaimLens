using Microsoft.AspNetCore.Mvc;

namespace ClaimLens.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse("ok", "claimlens-api"));
    }
}

public sealed record HealthResponse(string Status, string Service);
