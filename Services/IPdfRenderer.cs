public interface IPdfRenderer
{
    /// <exception cref="InvalidPdfRequestException">The request cannot be rendered as specified.</exception>
    Task<PdfResponse> RenderAsync(PdfRequest request, CancellationToken cancellationToken);
}

/// <summary>Raised when a request is well-formed but cannot be rendered; the message is safe to show to the caller.</summary>
public sealed class InvalidPdfRequestException(string message) : Exception(message);
