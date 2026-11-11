using System.Security.Claims;
using ClaimLens.Application.Common;

namespace ClaimLens.Api.Auth;

public static class CurrentUserId
{
    public static Guid From(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
        {
            throw new AppException("UNAUTHORIZED", 401, "Sign in is required.");
        }

        return userId;
    }
}
