using System.Text.Json.Serialization;

// PDFsharp 6.5.0+ requires choosing a license before creating any document.
PdfSharp.PdfSharpLicense.SetAgplLicense();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHttpClient("stylesheets", c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddSingleton<IPdfRenderer, PdfRenderer>();

var app = builder.Build();

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

app.Run();
