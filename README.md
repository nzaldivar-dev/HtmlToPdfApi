# HtmlToPdfApi

API local (ASP.NET Core, .NET 10, Minimal API) que convierte HTML a PDF y lo devuelve como Data URL (base64). Pensada para ser llamada desde un backend en el mismo servidor. Soporta hojas A3/A4/A5/Letter/Legal, tickets térmicos (80 mm, 58 mm o ancho personalizado) y hojas de estilo por URL.

El PDF se genera en memoria: no se escribe ningún archivo en disco.

## Tecnología

- [PDFsharp](https://docs.pdfsharp.net/) 6.5.0 para generar el PDF. Desde esta versión exige elegir licencia; el proyecto usa la **AGPL** (`PdfSharpLicense.SetAgplLicense()` en `Program.cs`). Si el servicio forma parte de un producto cerrado, valida las implicaciones de la AGPL o adquiere una licencia comercial (`PdfSharpLicense.LoadLicense`).
- [HtmlRenderer.PdfSharp](https://www.nuget.org/packages/HtmlRenderer.PdfSharp) 1.6.1 para interpretar el HTML, porque PDFsharp por sí solo no lee HTML.

## Requisitos

- .NET SDK 10
- Windows (usa las fuentes del sistema). En Linux haría falta configurar un font resolver de PDFsharp.

## Ejecutar

```
dotnet run
```

Escucha en `http://localhost:5072` (sin HTTPS ni autenticación, solo para uso local). Swagger UI está en `http://localhost:5072/swagger` y el documento OpenAPI en `/openapi/v1.json` (ambos en todos los entornos). `HtmlToPdfApi.http` contiene ejemplos listos para ejecutar.

## Endpoint

### `POST /api/pdf`

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `html` | string, requerido | Contenido HTML a renderizar. |
| `paperSize` | string | `A3`, `A4` (por defecto), `A5`, `Letter`, `Legal`, `Thermal80`, `Thermal58`, `Thermal`, `Custom`. No distingue mayúsculas. |
| `widthMm` | número | Ancho en mm (10–1000). Requerido para `Thermal` y `Custom`; se ignora en los demás formatos (se usa el tamaño predefinido). |
| `heightMm` | número | Alto en mm (10–5000). Requerido solo para `Custom`; se ignora en los demás formatos (los térmicos ajustan el alto al contenido). |
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
{
  "dataUrl": "data:application/pdf;base64,JVBERi0xLjc...",
  "extension": "pdf",
  "sizeBytes": 2534,
  "pages": 1,
  "widthMm": 80,
  "heightMm": 80.4
}
```

`sizeBytes` es el tamaño del PDF antes de codificarlo en base64.

Los errores siguen [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) (Problem Details) y se devuelven con `Content-Type: application/problem+json`. Todos incluyen `type`, `title`, `status`, `instance` (ruta de la petición) y el campo de extensión `traceId`.

| `type` | Estado | Cuándo |
|--------|--------|--------|
| `urn:htmltopdfapi:problem:malformed-body` | 400 | JSON inválido, campo requerido faltante o `paperSize` desconocido. |
| `urn:htmltopdfapi:problem:validation-error` | 400 | Falla una validación de rango; el campo `errors` indica cuáles. |
| `urn:htmltopdfapi:problem:invalid-request` | 400 | Falta `widthMm`/`heightMm` para el formato, URL de estilos inválida o no descargable, o host no permitido; `detail` explica el motivo. |
| Los de RFC 9110 | 404, 405, 415, 500 | Errores genéricos del framework. |

Ejemplo:

```json
{
  "type": "urn:htmltopdfapi:problem:invalid-request",
  "title": "The request cannot be rendered",
  "status": 400,
  "detail": "WidthMm is required for this paper size.",
  "instance": "/api/pdf",
  "traceId": "00-173b5aa12be4ed5cbaf0ae11d4cae161-ec9471d62cb539ac-00"
}
```

Para guardar el PDF desde PowerShell:

```powershell
$r = Invoke-RestMethod http://localhost:5072/api/pdf -Method Post -ContentType 'application/json' -Body $json
[IO.File]::WriteAllBytes("ticket.$($r.extension)", [Convert]::FromBase64String($r.dataUrl.Split(',')[1]))
```

## Configuración

Opcional, en `appsettings.json`: restringir los hosts desde los que se pueden descargar hojas de estilo (vacío = cualquiera).

```json
{ "PdfApi": { "AllowedStyleSheetHosts": ["cdn.jsdelivr.net", "intranet.local"] } }
```

## Logging

Usa [Serilog](https://serilog.net/) configurado completamente desde la sección `Serilog` de `appsettings.json` (y `appsettings.Development.json`, que sube el nivel a `Debug`). Escribe en consola y en archivo con rotación:

| Clave (`Serilog:WriteTo:1:Args`) | Por defecto | Efecto |
|----------------------------------|-------------|--------|
| `path` | `logs/htmltopdfapi-.log` | Ruta base; Serilog agrega la fecha (`htmltopdfapi-20261023.log`). Es relativa al directorio de trabajo: si corre como servicio, usa una ruta absoluta. |
| `rollingInterval` | `Day` | Un archivo por día (también acepta `Hour`, `Month`, ...). |
| `retainedFileCountLimit` | `5` | Máximo histórico: se conservan los 5 archivos más recientes (5 días) y se borran los anteriores. `null` = sin límite. |
| `fileSizeLimitBytes` + `rollOnFileSizeLimit` | `null` + `false` | Sin rotación por tamaño, para que 1 archivo = 1 día. Si la activas (por ejemplo 10485760 y `true`), cada día puede generar varios archivos (`..._001.log`) y el límite de 5 archivos cubrirá menos días. |
| `outputTemplate` | ver `appsettings.json` | Formato de cada línea. |

Los niveles se ajustan en `Serilog:MinimumLevel`. Cada línea incluye máquina, proceso, hilo y `Application`, y las peticiones HTTP añaden `RequestId`, `TraceId` (el mismo de `traceId` en los errores RFC 9457), IP remota, `UserAgent` y tamaño del cuerpo. Por cada PDF se registra papel, páginas, tamaño y duración; **nunca** el contenido HTML. Cualquier clave se puede sobreescribir con variables de entorno, por ejemplo `Serilog__WriteTo__1__Args__retainedFileCountLimit=7`.

## Limitaciones

- HtmlRenderer soporta HTML 4 y CSS 2.1: no hay flexbox, grid ni otras características de CSS3 moderno.
- Un ticket muy corto sale con alto mínimo igual a su ancho (por ejemplo 80 × 80.4 mm).
- El alto máximo de página es 5080 mm (límite de PDF); un contenido más largo se parte en varias páginas.