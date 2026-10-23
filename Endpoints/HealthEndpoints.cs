using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public static class HealthEndpoints
{
    public static void MapHealth(this WebApplication app)
    {
        app.MapGet("/health", async Task<Results<Ok<string>, JsonHttpResult<string>>> (HealthCheckService health, CancellationToken cancellationToken) =>
            {
                var report = await health.CheckHealthAsync(cancellationToken);
                var status = report.Status.ToString();
                return report.Status == HealthStatus.Healthy
                    ? TypedResults.Ok(status)
                    : TypedResults.Json(status, statusCode: StatusCodes.Status503ServiceUnavailable);
            })
            .WithName("Health")
            .WithSummary("Liveness check")
            .WithDescription("Returns 200 with the status 'Healthy' while the service is running, or 503 if a health check fails.");
    }
}
