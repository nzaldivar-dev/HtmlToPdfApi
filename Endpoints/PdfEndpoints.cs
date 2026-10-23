using Microsoft.AspNetCore.Http.HttpResults;

public static class PdfEndpoints
{
    public static void MapPdf(this WebApplication app)
    {
        app.MapPost("/api/pdf", async Task<Ok<PdfResponse>> (PdfRequest request, IPdfRenderer renderer, CancellationToken cancellationToken) =>
                TypedResults.Ok(await renderer.RenderAsync(request, cancellationToken)))
            .WithName("GeneratePdf")
            .WithSummary("Generate a PDF from HTML")
            .WithDescription("Renders the HTML (plus optional stylesheets) on the requested paper size or thermal ticket and returns the PDF as a base64 Data URL with its extension and size. Nothing is written to disk.")
            .Produces<PdfResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
