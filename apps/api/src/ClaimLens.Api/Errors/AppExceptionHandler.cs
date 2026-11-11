using System.Text.Json;
using ClaimLens.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace ClaimLens.Api.Errors;

public sealed class AppExceptionHandler : IExceptionHandler
{
    private readonly ILogger<AppExceptionHandler> _logger;

    public AppExceptionHandler(ILogger<AppExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ValidationAppException validation)
        {
            httpContext.Response.StatusCode = validation.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                code = validation.Code,
                message = validation.Message,
                details = validation.Details.Select(detail => new
                {
                    field = JsonNamingPolicy.CamelCase.ConvertName(detail.Field),
                    message = detail.Message
                })
            }, cancellationToken);
            return true;
        }

        if (exception is AppException appException)
        {
            httpContext.Response.StatusCode = appException.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                code = appException.Code,
                message = appException.Message
            }, cancellationToken);
            return true;
        }

        _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            code = "INTERNAL_ERROR",
            message = "Something went wrong."
        }, cancellationToken);
        return true;
    }
}
