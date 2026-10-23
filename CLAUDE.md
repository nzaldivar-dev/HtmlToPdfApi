# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Local-only ASP.NET Core (net10.0) Minimal API that converts HTML to PDF and returns it as base64. It is called by a backend on the same server; there is no auth and no HTTPS (`http://localhost:5072`). PDFs are built in memory — nothing is written to disk.

## Commands

```
dotnet build
dotnet run                      # http://localhost:5072, OpenAPI at /openapi/v1.json (Development only)
```

`HtmlToPdfApi.http` has sample requests for the single endpoint, `POST /api/pdf`. There is no test project.

`dotnet build` always prints NU1902/NU1903 for `System.Security.Cryptography.Pkcs` 8.0.1, a transitive dependency of HtmlRenderer; pinning 10.0.10 did not clear it, so they are deliberately left in.

## Architecture

- **PDFsharp cannot parse HTML.** HTML rendering comes from `HtmlRenderer.PdfSharp` (HTML 4 / CSS 2.1 only — no flexbox/grid), which drives PDFsharp 6.2.4. `PdfRenderer` (`Services/PdfRenderer.cs`) is the only place that touches either library.
- `Program.cs` wires DI and `Endpoints/PdfEndpoints.cs` (`MapPdf`); the handler only forwards to `IPdfRenderer`. Add new endpoints as one static `Map<Resource>` class per resource.
- Caller-facing errors: throw `InvalidPdfRequestException` from the service (message is shown to the caller). `Middleware/ApiExceptionHandler.cs` turns it, and framework `BadHttpRequestException` (bad enum/missing field), into 400 Problem Details; anything else falls through to a 500. `[Required]`/`[Range]` on `PdfRequest` are enforced via `AddValidation()`.
- Enums serialize as strings (`JsonStringEnumConverter`). Case-sensitive/strict JSON options were intentionally *not* enabled, so PascalCase callers keep working.

## Rendering behavior that is easy to break

- **Thermal tickets** (`Thermal80`, `Thermal58`, `Thermal`): when `heightMm` is omitted, height is measured with a throwaway `HtmlContainer` layout pass (`MeasureHeight`), then the page is regenerated taller (+5% +20pt, up to 4 retries) until it fits on one page, since the measurement can come up slightly short.
- HtmlRenderer swaps width/height when width > height, so page height is clamped to at least `width + 1pt` (a short ticket comes out ~80×80.4 mm). Max page height is 14400pt (PDF limit); taller content paginates.
- `GlobalFontSettings.UseWindowsFontsUnderWindows` is set in a static constructor; on Linux a font resolver would be required.
- Stylesheets are downloaded server-side (`LoadStyleSheetsAsync`), http/https only, 15s timeout (named `"stylesheets"` HttpClient). Setting `PdfApi:AllowedStyleSheetHosts` (string array) in configuration restricts allowed hosts; empty means any.
