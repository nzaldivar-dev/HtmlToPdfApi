using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Messages of InvalidPdfRequestException are written for callers; framework binding errors are not, so use a generic one.
        var (type, title, detail) = exception switch
        {
            InvalidPdfRequestException => (ProblemTypes.InvalidRequest, "The request cannot be rendered", exception.Message),
            BadHttpRequestException => (ProblemTypes.MalformedBody, "The request body is invalid",
                "Check required fields and that paperSize is one of: A3, A4, A5, Letter, Legal, Thermal80, Thermal58, Thermal, Custom."),
            _ => (null, null, null)
        };

        if (type is null)
            return false; // Let the default handler return a 500 problem details.

        // Expected client errors: no stack trace needed.
        logger.LogWarning("Rejected PDF request ({ExceptionType}): {Reason}", exception.GetType().Name, exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        // Written through IProblemDetailsService so the response is application/problem+json and gets the shared extensions.
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Type = type,
                Title = title,
                Status = StatusCodes.Status400BadRequest,
                Detail = detail
            }
        });
    }
}
