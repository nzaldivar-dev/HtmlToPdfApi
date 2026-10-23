using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using TheArtOfDev.HtmlRenderer.Core;
using TheArtOfDev.HtmlRenderer.PdfSharp;

public sealed class PdfRenderer(IHttpClientFactory httpClientFactory, IConfiguration config) : IPdfRenderer
{
    const double PtPerMm = 72.0 / 25.4;
    const double MaxPageHeightPt = 14400; // limite de PDF (200 in)

    static PdfRenderer()
    {
        if (OperatingSystem.IsWindows())
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
    }

    public async Task<PdfResponse> RenderAsync(PdfRequest req, CancellationToken ct)
    {
        var css = await LoadStyleSheetsAsync(req.StyleSheetUrls, ct);
        var cssData = css.Length > 0 ? PdfGenerator.ParseStyleSheet(css) : null;

        var paper = req.PaperSize;
        var thermal = paper is PaperSize.Thermal80 or PaperSize.Thermal58 or PaperSize.Thermal;
        var margin = (req.MarginMm ?? (thermal ? 2 : 10)) * PtPerMm;

        var pdfConfig = new PdfGenerateConfig { PageOrientation = req.Landscape ? PageOrientation.Landscape : PageOrientation.Portrait };
        pdfConfig.MarginLeft = pdfConfig.MarginRight = pdfConfig.MarginTop = pdfConfig.MarginBottom = (int)Math.Round(margin);

        var autoHeight = false;
        var heightPt = 0.0;
        if (thermal || paper == PaperSize.Custom)
        {
            var widthMm = paper switch
            {
                PaperSize.Thermal80 => req.WidthMm ?? 80,
                PaperSize.Thermal58 => req.WidthMm ?? 58,
                _ => req.WidthMm ?? throw new InvalidPdfRequestException("WidthMm is required for this paper size.")
            };
            var widthPt = widthMm * PtPerMm;
            autoHeight = req.HeightMm is null && thermal;
            heightPt = req.HeightMm is { } h
                ? h * PtPerMm
                : thermal
                    ? MeasureHeight(req.Html, cssData, widthPt, pdfConfig) // ticket continuo: alto = contenido
                    : throw new InvalidPdfRequestException("HeightMm is required for Custom paper.");

            // HtmlRenderer intercambia las medidas si ancho > alto, asi que el alto minimo es el ancho.
            pdfConfig.ManualPageSize = new XSize(widthPt, Math.Clamp(heightPt, widthPt + 1, MaxPageHeightPt));
        }
        else
        {
            pdfConfig.PageSize = paper switch
            {
                PaperSize.A3 => PdfSharp.PageSize.A3,
                PaperSize.A4 => PdfSharp.PageSize.A4,
                PaperSize.A5 => PdfSharp.PageSize.A5,
                PaperSize.Letter => PdfSharp.PageSize.Letter,
                _ => PdfSharp.PageSize.Legal
            };
        }

        var doc = PdfGenerator.GeneratePdf(req.Html, pdfConfig, cssData);

        // La medicion puede quedar corta por redondeos de layout: crece hasta que quepa en una pagina.
        for (var i = 0; autoHeight && doc.PageCount > 1 && heightPt < MaxPageHeightPt && i < 4; i++)
        {
            doc.Dispose();
            heightPt = Math.Min(heightPt * 1.05 + 20, MaxPageHeightPt);
            pdfConfig.ManualPageSize = new XSize(pdfConfig.ManualPageSize.Width, heightPt);
            doc = PdfGenerator.GeneratePdf(req.Html, pdfConfig, cssData);
        }
        using var _ = doc;

        var pages = doc.PageCount;
        var page = doc.Pages[0];
        var (wMm, hMm) = (Math.Round(page.Width.Point / PtPerMm, 1), Math.Round(page.Height.Point / PtPerMm, 1));

        using var ms = new MemoryStream();
        doc.Save(ms, false);

        return new PdfResponse(Convert.ToBase64String(ms.ToArray()), pages, wMm, hMm);
    }

    /// <summary>Calcula el alto del contenido para un ticket continuo.</summary>
    static double MeasureHeight(string html, CssData? cssData, double widthPt, PdfGenerateConfig cfg)
    {
        using var doc = new PdfDocument();
        var page = doc.AddPage();
        page.Width = XUnit.FromPoint(widthPt);
        page.Height = XUnit.FromPoint(MaxPageHeightPt);
        using var gfx = XGraphics.FromPdfPage(page);

        using var container = new HtmlContainer();
        container.MaxSize = new XSize(widthPt - cfg.MarginLeft - cfg.MarginRight, 0);
        container.SetHtml(html, cssData);
        container.PerformLayout(gfx);

        return container.ActualSize.Height + cfg.MarginTop + cfg.MarginBottom;
    }

    async Task<string> LoadStyleSheetsAsync(string[]? urls, CancellationToken ct)
    {
        if (urls is not { Length: > 0 }) return "";

        var allowed = config.GetSection("PdfApi:AllowedStyleSheetHosts").Get<string[]>() ?? [];
        var http = httpClientFactory.CreateClient("stylesheets");
        var parts = new List<string>();

        foreach (var raw in urls)
        {
            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                throw new InvalidPdfRequestException($"Invalid stylesheet URL: '{raw}'.");
            if (allowed.Length > 0 && !allowed.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
                throw new InvalidPdfRequestException($"Stylesheet host not allowed: '{uri.Host}'.");

            try
            {
                parts.Add(await http.GetStringAsync(uri, ct));
            }
            catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !ct.IsCancellationRequested))
            {
                throw new InvalidPdfRequestException($"Could not download stylesheet '{raw}'.");
            }
        }

        return string.Join("\n", parts);
    }
}
