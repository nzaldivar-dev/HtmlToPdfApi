/// <summary>Generated PDF.</summary>
/// <param name="DataUrl">PDF as a Data URL: <c>data:application/pdf;base64,...</c>.</param>
/// <param name="Extension">File extension without the dot.</param>
/// <param name="SizeBytes">Size of the PDF file in bytes (before base64 encoding).</param>
/// <param name="Pages">Number of pages in the document.</param>
/// <param name="WidthMm">Page width in millimeters.</param>
/// <param name="HeightMm">Page height in millimeters.</param>
public sealed record PdfResponse(string DataUrl, string Extension, long SizeBytes, int Pages, double WidthMm, double HeightMm);
