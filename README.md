# HtmlToPdfApi

API local (ASP.NET Core, .NET 10, Minimal API) que convierte HTML a PDF y lo devuelve en base64. Pensada para ser llamada desde un backend en el mismo servidor. Soporta hojas A3/A4/A5/Letter/Legal, tickets térmicos (80 mm, 58 mm o ancho personalizado) y hojas de estilo por URL.

El PDF se genera en memoria: no se escribe ningún archivo en disco.

## Tecnología

- [PDFsharp](https://docs.pdfsharp.net/) 6.2.4 para generar el PDF.
- [HtmlRenderer.PdfSharp](https://www.nuget.org/packages/HtmlRenderer.PdfSharp) 1.6.1 para interpretar el HTML, porque PDFsharp por sí solo no lee HTML.

## Requisitos

- .NET SDK 10
- Windows (usa las fuentes del sistema). En Linux haría falta configurar un font resolver de PDFsharp.

## Ejecutar

```
dotnet run
```

Escucha en `http://localhost:5072` (sin HTTPS ni autenticación, solo para uso local). En Development, el documento OpenAPI está en `/openapi/v1.json`. `HtmlToPdfApi.http` contiene ejemplos listos para ejecutar.

## Endpoint

### `POST /api/pdf`

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `html` | string, requerido | Contenido HTML a renderizar. |
| `paperSize` | string | `A3`, `A4` (por defecto), `A5`, `Letter`, `Legal`, `Thermal80`, `Thermal58`, `Thermal`, `Custom`. No distingue mayúsculas. |
| `widthMm` | número | Ancho en mm (10–1000). Requerido para `Thermal` y `Custom`; en los demás formatos térmicos sustituye el ancho predefinido. |
| `heightMm` | número | Alto en mm (10–5000). Requerido para `Custom`. En formatos térmicos, si se omite el alto se ajusta al contenido. |
| `landscape` | bool | Orientación horizontal para hojas. |
| `marginMm` | número | Margen en mm (0–50). Por defecto 10 en hojas y 2 en tickets. |
| `styleSheetUrls` | string[] | URLs `http`/`https` de hojas de estilo que se descargan y se aplican. |

Ejemplo:

```json
{
  "html": "<h3 style='text-align:center'>TICKET #1</h3><p>Item A ..... $10.00</p><hr/><b>Total $15.00</b>",
  "paperSize": "Thermal80",
  "styleSheetUrls": ["https://cdn.jsdelivr.net/npm/normalize.css@8.0.1/normalize.css"]
}
```

Respuesta `200`:

```json
{ "base64": "JVBERi0xLjc...", "pages": 1, "widthMm": 80, "heightMm": 80.4 }
```

Los errores se devuelven como Problem Details (RFC 7807) con estado `400`: cuerpo inválido, `paperSize` desconocido, campos requeridos faltantes, URL de estilos inválida o no descargable, o host no permitido.

Para guardar el PDF desde PowerShell:

```powershell
$r = Invoke-RestMethod http://localhost:5072/api/pdf -Method Post -ContentType 'application/json' -Body $json
[IO.File]::WriteAllBytes("ticket.pdf", [Convert]::FromBase64String($r.base64))
```

## Configuración

Opcional, en `appsettings.json`: restringir los hosts desde los que se pueden descargar hojas de estilo (vacío = cualquiera).

```json
{ "PdfApi": { "AllowedStyleSheetHosts": ["cdn.jsdelivr.net", "intranet.local"] } }
```

## Limitaciones

- HtmlRenderer soporta HTML 4 y CSS 2.1: no hay flexbox, grid ni otras características de CSS3 moderno.
- Un ticket muy corto sale con alto mínimo igual a su ancho (por ejemplo 80 × 80.4 mm).
- El alto máximo de página es 5080 mm (límite de PDF); un contenido más largo se parte en varias páginas.
- `dotnet build` muestra los avisos NU1902/NU1903 de `System.Security.Cryptography.Pkcs`, dependencia transitiva de HtmlRenderer.
