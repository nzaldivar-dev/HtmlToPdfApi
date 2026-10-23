using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Messages of InvalidPdfRequestException are written for callers; framework binding errors are not, so use a generic one.
        var detail = exception switch
        {
            InvalidPdfRequestException => exception.Message,
            BadHttpRequestException => "The request body is invalid. Check required fields and that paperSize is one of: A3, A4, A5, Letter, Legal, Thermal80, Thermal58, Thermal, Custom.",
            _ => null
        };

        if (detail is null)
            return false; // Let the default handler return a 500 problem details.

        logger.LogWarning(exception, "Invalid PDF request");

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = detail,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }
}
