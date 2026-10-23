using System.ComponentModel.DataAnnotations;

/// <summary>Payload for generating a PDF from HTML.</summary>
public sealed record PdfRequest
{
    /// <summary>HTML content to render (HTML 4 / CSS 2.1).</summary>
    [Required]
    public required string Html { get; init; }

    /// <summary>Paper format. Defaults to A4.</summary>
    public PaperSize PaperSize { get; init; } = PaperSize.A4;

    /// <summary>Page width in millimeters. Required for Thermal and Custom; ignored for every other paper size (preset width is used).</summary>
    [Range(10, 1000)]
    public double? WidthMm { get; init; }

    /// <summary>Page height in millimeters. Required for Custom only; ignored for every other paper size (thermal formats fit the content height).</summary>
    [Range(10, 5000)]
    public double? HeightMm { get; init; }

    /// <summary>Renders sheet formats in landscape orientation.</summary>
    public bool Landscape { get; init; }

    /// <summary>Page margin in millimeters. Defaults to 10 for sheets and 2 for thermal formats.</summary>
    [Range(0, 50)]
    public double? MarginMm { get; init; }

    /// <summary>Absolute http/https URLs of stylesheets applied to the HTML.</summary>
    public string[]? StyleSheetUrls { get; init; }
}
