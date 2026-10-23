using System.Diagnostics;
using System.Text.Json.Serialization;
using Serilog;

// Logs startup failures until the configured logger (appsettings "Serilog" section) takes over.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

// PDFsharp 6.5.0+ requires choosing a license before creating any document.
PdfSharp.PdfSharpLicense.SetAgplLicense();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    var problem = context.ProblemDetails;

    // RFC 9457: every problem identifies the occurrence (instance) and carries a correlation id as an extension member.
    problem.Instance ??= context.HttpContext.Request.Path;
    problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

    if (problem is HttpValidationProblemDetails)
        problem.Type = ProblemTypes.ValidationError;
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHttpClient("stylesheets", c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddSingleton<IPdfRenderer, PdfRenderer>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier);
        diagnosticContext.Set("RemoteIp", httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("RequestLength", httpContext.Request.ContentLength);
    });
app.UseExceptionHandler();
app.UseStatusCodePages();

// Docs are available in every environment: the API is internal and its callers need the contract.
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "HtmlToPdfApi v1");
    options.RoutePrefix = "swagger";
});

app.MapPdf();
app.MapHealth();

try
{
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "HtmlToPdfApi terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
