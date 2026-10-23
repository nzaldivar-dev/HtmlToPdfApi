/// <summary>Generated PDF.</summary>
/// <param name="Base64">PDF file encoded as base64.</param>
/// <param name="Pages">Number of pages in the document.</param>
/// <param name="WidthMm">Page width in millimeters.</param>
/// <param name="HeightMm">Page height in millimeters.</param>
public sealed record PdfResponse(string Base64, int Pages, double WidthMm, double HeightMm);
