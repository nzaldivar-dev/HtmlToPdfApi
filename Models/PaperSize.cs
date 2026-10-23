/// <summary>Paper formats supported by the PDF generator.</summary>
public enum PaperSize
{
    /// <summary>ISO A3 sheet.</summary>
    A3,
    /// <summary>ISO A4 sheet.</summary>
    A4,
    /// <summary>ISO A5 sheet.</summary>
    A5,
    /// <summary>US Letter sheet.</summary>
    Letter,
    /// <summary>US Legal sheet.</summary>
    Legal,
    /// <summary>80 mm thermal ticket; height fits the content.</summary>
    Thermal80,
    /// <summary>58 mm thermal ticket; height fits the content.</summary>
    Thermal58,
    /// <summary>Thermal ticket with a custom WidthMm; height fits the content.</summary>
    Thermal,
    /// <summary>Fixed size given by WidthMm and HeightMm.</summary>
    Custom
}
