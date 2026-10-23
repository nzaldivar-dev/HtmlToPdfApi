# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Local-only ASP.NET Core (net10.0) Minimal API that converts HTML to PDF and returns it as a Data URL (`data:application/pdf;base64,...`) plus `extension` and `sizeBytes`. It is called by a backend on the same server; there is no auth and no HTTPS (`http://localhost:5072`). PDFs are built in memory — nothing is written to disk.

## Commands

```
dotnet build
dotnet run                      # http://localhost:5072, Swagger UI at /swagger, OpenAPI at /openapi/v1.json (all environments)
```

`HtmlToPdfApi.http` has sample requests for `POST /api/pdf` and the `GET /health` liveness check (`Endpoints/HealthEndpoints.cs`, built-in `AddHealthChecks`; returns 200 `Healthy`, no PDF render). There is no test project.

`System.Security.Cryptography.Pkcs` is referenced directly (10.0.13) only to override the vulnerable 8.0.1 that HtmlRenderer pulls in transitively (NU1902/NU1903); keep it.

`dotnet build` fails with MSB3021/MSB3027 (cannot copy `HtmlToPdfApi.exe`) if the API is still running; stop it first.

## Architecture

- **PDFsharp cannot parse HTML.** HTML rendering comes from `HtmlRenderer.PdfSharp` (HTML 4 / CSS 2.1 only — no flexbox/grid), which drives PDFsharp. `PDFsharp` 6.5.0 is referenced directly (HtmlRenderer only asks for 6.2.4) and, since 6.5.0, a license must be selected before any `PdfDocument` is created — `Program.cs` calls `SetAgplLicense()`; without it every render throws `PdfSharpLicenseException`. Swagger UI is `Swashbuckle.AspNetCore.SwaggerUI` only, pointed at the built-in `AddOpenApi()` document (no Swashbuckle generator, which is incompatible with .NET 10 OpenAPI types). `PdfRenderer` (`Services/PdfRenderer.cs`) is the only place that touches either library.
- `Program.cs` wires DI and `Endpoints/PdfEndpoints.cs` (`MapPdf`); the handler only forwards to `IPdfRenderer`. Add new endpoints as one static `Map<Resource>` class per resource.
- Caller-facing errors: throw `InvalidPdfRequestException` from the service (message is shown to the caller). `Middleware/ApiExceptionHandler.cs` turns it, and framework `BadHttpRequestException` (bad enum/missing field), into 400 Problem Details (RFC 9457); anything else falls through to a 500. The handler writes through `IProblemDetailsService` (not `WriteAsJsonAsync`) so the response is `application/problem+json`; `CustomizeProblemDetails` in `Program.cs` adds `instance` and `traceId` to every problem (including 404/405/415 from `UseStatusCodePages`) and maps validation errors to our `type`. Custom problem type URIs live in `Middleware/ProblemTypes.cs`. `[Required]`/`[Range]` on `PdfRequest` are enforced via `AddValidation()`.
- Logging is Serilog, fully driven by the `Serilog` section of `appsettings*.json` (`ReadFrom.Configuration`): file path, `rollingInterval`, `fileSizeLimitBytes`, `retainedFileCountLimit`, enrichers and levels are config, not code. The file sink is `WriteTo[1]` (index matters for env-var overrides like `Serilog__WriteTo__1__Args__...`). `UseSerilogRequestLogging` sits before `UseExceptionHandler` in `Program.cs`. Never log request HTML (size/PII); log metadata only.
- Enums serialize as strings (`JsonStringEnumConverter`). Case-sensitive/strict JSON options were intentionally *not* enabled, so PascalCase callers keep working.

## Rendering behavior that is easy to break

- **Thermal tickets** (`Thermal80`, `Thermal58`, `Thermal`; `widthMm` is only read for `Thermal`/`Custom`, presets ignore it): `heightMm` is ignored and height is measured with a throwaway `HtmlContainer` layout pass (`MeasureHeight`), then the page is regenerated taller (+5% +20pt, up to 4 retries) until it fits on one page, since the measurement can come up slightly short.
- HtmlRenderer swaps width/height when width > height, so page height is clamped to at least `width + 1pt` (a short ticket comes out ~80×80.4 mm). Max page height is 14400pt (PDF limit); taller content paginates.
- `GlobalFontSettings.UseWindowsFontsUnderWindows` is set in a static constructor; on Linux a font resolver would be required.
- Stylesheets are downloaded server-side (`LoadStyleSheetsAsync`), http/https only, 15s timeout (named `"stylesheets"` HttpClient). Setting `PdfApi:AllowedStyleSheetHosts` (string array) in configuration restricts allowed hosts; empty means any.
