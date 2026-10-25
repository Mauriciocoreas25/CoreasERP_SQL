using System.Diagnostics;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using CoreasERP.Data;
using CoreasERP.Models;

namespace CoreasERP.Services;

public class ExcelExportResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string? RutaArchivoLocal { get; set; }
    public byte[] ContenidoBytes { get; set; } = Array.Empty<byte>();
    public string Base64Data => Convert.ToBase64String(ContenidoBytes);
}

public class ExcelImportResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int TotalProcesados { get; set; }
    public int TotalInsertados { get; set; }
    public int TotalActualizados { get; set; }
    public List<string> Errores { get; set; } = new();
}

public interface IExcelExportService
{
    Task<ExcelExportResult> ExportarLibrosIvaAsync(string periodo = "Mes", DateTime? fechaDesde = null, DateTime? fechaHasta = null);
    Task<ExcelExportResult> ExportarAlertasStockBajoAsync();
    Task<ExcelExportResult> ExportarInventarioCompletoAsync();
    Task<ExcelImportResult> ImportarInventarioExcelAsync(Stream fileStream);
    byte[] GenerarPlantillaImportacionExcel();
    ExcelExportResult DescargarPlantillaImportacion();
    bool AbrirArchivo(string rutaArchivo);
    bool AbrirCarpeta(string rutaArchivo);
}

public class ExcelExportService : IExcelExportService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ExcelExportService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ExcelExportResult> ExportarLibrosIvaAsync(string periodo = "Mes", DateTime? fechaDesde = null, DateTime? fechaHasta = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            DateTime? fechaInicio = null;
            DateTime? fechaFin = null;
            DateTime ahora = DateTime.Now;

            switch (periodo)
            {
                case "Hoy":
                    fechaInicio = DateTime.Today;
                    fechaFin = DateTime.Today.AddDays(1).AddTicks(-1);
                    break;
                case "Mes":
                    fechaInicio = new DateTime(ahora.Year, ahora.Month, 1);
                    fechaFin = new DateTime(ahora.Year, ahora.Month, 1).AddMonths(1).AddTicks(-1);
                    break;
                case "Año":
                    fechaInicio = new DateTime(ahora.Year, 1, 1);
                    fechaFin = new DateTime(ahora.Year + 1, 1, 1).AddTicks(-1);
                    break;
                case "Personalizado":
                    if (fechaDesde.HasValue) fechaInicio = fechaDesde.Value.Date;
                    if (fechaHasta.HasValue) fechaFin = fechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                    break;
                case "Todos":
                default:
                    fechaInicio = null;
                    fechaFin = null;
                    break;
            }

            // Consultar ventas reales del período
            var ventasQuery = context.Ventas
                .Include(v => v.Cliente)
                .Where(v => v.Estado == "Completada");

            if (fechaInicio.HasValue)
            {
                ventasQuery = ventasQuery.Where(v => v.FechaHora >= fechaInicio.Value);
            }
            if (fechaFin.HasValue)
            {
                ventasQuery = ventasQuery.Where(v => v.FechaHora <= fechaFin.Value);
            }

            var ventas = await ventasQuery
                .OrderBy(v => v.FechaHora)
                .ToListAsync();

            // Consultar compras reales del período
            var comprasQuery = context.Compras
                .Include(c => c.Proveedor)
                .Where(c => c.Estado == "Completada");

            if (fechaInicio.HasValue)
            {
                comprasQuery = comprasQuery.Where(c => c.FechaHora >= fechaInicio.Value);
            }
            if (fechaFin.HasValue)
            {
                comprasQuery = comprasQuery.Where(c => c.FechaHora <= fechaFin.Value);
            }

            var compras = await comprasQuery
                .OrderBy(c => c.FechaHora)
                .ToListAsync();

            // Cálculos tributarios (13% IVA El Salvador)
            decimal totalVentas = ventas.Sum(v => v.Total);
            decimal baseDebitoFiscal = ventas.Sum(v => v.Subtotal);
            decimal debitoFiscal = ventas.Sum(v => v.Impuesto);

            decimal totalCompras = compras.Sum(c => c.Total);
            var (baseCreditoFiscal, creditoFiscal) = totalCompras > 0 ? FinancialRoundingHelper.DesglosarIvaIncluido(totalCompras) : (0.00m, 0.00m);
            decimal balanceIva = FinancialRoundingHelper.Round2(debitoFiscal - creditoFiscal);

            using var workbook = new XLWorkbook();

            // ==========================================
            // HOJA 1: RESUMEN Y LIQUIDACIÓN DE IVA
            // ==========================================
            var wsResumen = workbook.Worksheets.Add("Resumen y Liquidación IVA");
            wsResumen.ShowGridLines = true;

            // Encabezado principal
            wsResumen.Range("A1:F1").Merge().Value = "COREAS ERP - SISTEMA DE CONTROL FISCAL";
            wsResumen.Range("A1:F1").Style.Font.Bold = true;
            wsResumen.Range("A1:F1").Style.Font.FontSize = 16;
            wsResumen.Range("A1:F1").Style.Font.FontColor = XLColor.White;
            wsResumen.Range("A1:F1").Style.Fill.BackgroundColor = XLColor.FromArgb(15, 23, 42); // Navy 900
            wsResumen.Range("A1:F1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            wsResumen.Range("A2:F2").Merge().Value = $"LIBROS DE IVA (DÉBITO Y CRÉDITO FISCAL) - PERÍODO: {periodo.ToUpper()}";
            wsResumen.Range("A2:F2").Style.Font.Bold = true;
            wsResumen.Range("A2:F2").Style.Font.FontSize = 12;
            wsResumen.Range("A2:F2").Style.Fill.BackgroundColor = XLColor.FromArgb(241, 245, 249);
            wsResumen.Range("A2:F2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            wsResumen.Cell("A3").Value = $"Fecha de Emisión: {ahora:dd/MM/yyyy HH:mm:ss}";
            wsResumen.Cell("A3").Style.Font.Italic = true;
            wsResumen.Cell("A3").Style.Font.FontSize = 9;

            // TABLA 1: DÉBITO FISCAL (VENTAS)
            wsResumen.Range("A5:C5").Merge().Value = "1. LIBRO DE VENTAS (DÉBITO FISCAL)";
            wsResumen.Range("A5:C5").Style.Font.Bold = true;
            wsResumen.Range("A5:C5").Style.Font.FontColor = XLColor.White;
            wsResumen.Range("A5:C5").Style.Fill.BackgroundColor = XLColor.FromArgb(30, 64, 175); // Blue 800

            wsResumen.Cell("A6").Value = "Concepto";
            wsResumen.Cell("B6").Value = "Detalle";
            wsResumen.Cell("C6").Value = "Monto ($)";
            wsResumen.Range("A6:C6").Style.Font.Bold = true;
            wsResumen.Range("A6:C6").Style.Fill.BackgroundColor = XLColor.FromArgb(219, 234, 254);

            wsResumen.Cell("A7").Value = "Comprobantes Totales Emitidos";
            wsResumen.Cell("B7").Value = $"{ventas.Count} comprobantes";
            wsResumen.Cell("C7").Value = ventas.Count;

            wsResumen.Cell("A8").Value = "Créditos Fiscales Emitidos (Contribuyentes)";
            wsResumen.Cell("B8").Value = $"{ventas.Count(v => v.TipoComprobante == "Credito Fiscal")} CCF";
            wsResumen.Cell("C8").Value = ventas.Count(v => v.TipoComprobante == "Credito Fiscal");

            wsResumen.Cell("A9").Value = "Total Facturado Bruto (Ventas)";
            wsResumen.Cell("B9").Value = "Total cobrado a clientes";
            wsResumen.Cell("C9").Value = totalVentas;
            wsResumen.Cell("C9").Style.NumberFormat.Format = "$#,##0.00";

            wsResumen.Cell("A10").Value = "Base Gravada / Imponible Neta";
            wsResumen.Cell("B10").Value = "Ventas netas sin impuesto";
            wsResumen.Cell("C10").Value = baseDebitoFiscal;
            wsResumen.Cell("C10").Style.NumberFormat.Format = "$#,##0.00";

            wsResumen.Cell("A11").Value = "DÉBITO FISCAL GENERADO (13% IVA)";
            wsResumen.Cell("B11").Value = "Impuesto cobrado al contribuyente";
            wsResumen.Cell("C11").Value = debitoFiscal;
            wsResumen.Cell("C11").Style.NumberFormat.Format = "$#,##0.00";
            wsResumen.Range("A11:C11").Style.Font.Bold = true;
            wsResumen.Range("A11:C11").Style.Fill.BackgroundColor = XLColor.FromArgb(254, 240, 138);

            wsResumen.Range("A5:C11").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsResumen.Range("A5:C11").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // TABLA 2: CRÉDITO FISCAL (COMPRAS)
            wsResumen.Range("E5:G5").Merge().Value = "2. LIBRO DE COMPRAS (CRÉDITO FISCAL)";
            wsResumen.Range("E5:G5").Style.Font.Bold = true;
            wsResumen.Range("E5:G5").Style.Font.FontColor = XLColor.White;
            wsResumen.Range("E5:G5").Style.Fill.BackgroundColor = XLColor.FromArgb(22, 101, 52); // Green 800

            wsResumen.Cell("E6").Value = "Concepto";
            wsResumen.Cell("F6").Value = "Detalle";
            wsResumen.Cell("G6").Value = "Monto ($)";
            wsResumen.Range("E6:G6").Style.Font.Bold = true;
            wsResumen.Range("E6:G6").Style.Fill.BackgroundColor = XLColor.FromArgb(220, 252, 231);

            wsResumen.Cell("E7").Value = "Facturas de Compra Registradas";
            wsResumen.Cell("F7").Value = $"{compras.Count} compras";
            wsResumen.Cell("G7").Value = compras.Count;

            wsResumen.Cell("E8").Value = "Total Compras de Mercadería";
            wsResumen.Cell("F8").Value = "Total facturado por proveedores";
            wsResumen.Cell("G8").Value = totalCompras;
            wsResumen.Cell("G8").Style.NumberFormat.Format = "$#,##0.00";

            wsResumen.Cell("E9").Value = "Base Gravada / Imponible Deducible";
            wsResumen.Cell("F9").Value = "Costo de compra neto (sin IVA)";
            wsResumen.Cell("G9").Value = baseCreditoFiscal;
            wsResumen.Cell("G9").Style.NumberFormat.Format = "$#,##0.00";

            wsResumen.Cell("E10").Value = "CRÉDITO FISCAL DEDUCIBLE (13% IVA)";
            wsResumen.Cell("F10").Value = "Impuesto pagado recuperable";
            wsResumen.Cell("G10").Value = creditoFiscal;
            wsResumen.Cell("G10").Style.NumberFormat.Format = "$#,##0.00";
            wsResumen.Range("E10:G10").Style.Font.Bold = true;
            wsResumen.Range("E10:G10").Style.Fill.BackgroundColor = XLColor.FromArgb(187, 247, 208);

            wsResumen.Range("E5:G10").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsResumen.Range("E5:G10").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // TABLA 3: LIQUIDACIÓN TRIBUTARIA RESULTANTE
            wsResumen.Range("A14:G14").Merge().Value = "3. LIQUIDACIÓN NETA DEL PERÍODO (DÉBITO FISCAL - CRÉDITO FISCAL)";
            wsResumen.Range("A14:G14").Style.Font.Bold = true;
            wsResumen.Range("A14:G14").Style.Font.FontColor = XLColor.White;
            wsResumen.Range("A14:G14").Style.Fill.BackgroundColor = XLColor.FromArgb(71, 85, 105);

            wsResumen.Range("A15:D15").Merge().Value = "Total Débito Fiscal Acumulado (+):";
            wsResumen.Range("E15:G15").Merge().Value = debitoFiscal;
            wsResumen.Range("E15:G15").Style.NumberFormat.Format = "$#,##0.00";

            wsResumen.Range("A16:D16").Merge().Value = "Total Crédito Fiscal Deducible (-):";
            wsResumen.Range("E16:G16").Merge().Value = creditoFiscal;
            wsResumen.Range("E16:G16").Style.NumberFormat.Format = "$#,##0.00";

            string estadoBalance = balanceIva > 0 ? "IMPUESTO A PAGAR AL FISCO" : (balanceIva < 0 ? "REMANENTE DE CRÉDITO A FAVOR DEL CONTRIBUYENTE" : "BALANCE EN CERO");
            var colorBalance = balanceIva > 0 ? XLColor.FromArgb(254, 226, 226) : XLColor.FromArgb(220, 252, 231);

            wsResumen.Range("A17:D17").Merge().Value = $"RESULTADO FINAL: {estadoBalance}";
            wsResumen.Range("A17:D17").Style.Font.Bold = true;
            wsResumen.Range("A17:D17").Style.Fill.BackgroundColor = colorBalance;

            wsResumen.Range("E17:G17").Merge().Value = Math.Abs(balanceIva);
            wsResumen.Range("E17:G17").Style.Font.Bold = true;
            wsResumen.Range("E17:G17").Style.Font.FontSize = 12;
            wsResumen.Range("E17:G17").Style.Fill.BackgroundColor = colorBalance;
            wsResumen.Range("E17:G17").Style.NumberFormat.Format = "$#,##0.00";

            wsResumen.Range("A14:G17").Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            wsResumen.Range("A14:G17").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            wsResumen.Columns().AdjustToContents();

            // ==========================================
            // HOJA 2: DETALLE LIBRO DE VENTAS (DÉBITO FISCAL)
            // ==========================================
            var wsVentas = workbook.Worksheets.Add("Libro Ventas (Débito)");
            wsVentas.ShowGridLines = true;

            wsVentas.Range("A1:J1").Merge().Value = "LIBRO DE VENTAS A CONSUMIDORES Y CONTRIBUYENTES (DÉBITO FISCAL)";
            wsVentas.Range("A1:J1").Style.Font.Bold = true;
            wsVentas.Range("A1:J1").Style.Font.FontSize = 14;
            wsVentas.Range("A1:J1").Style.Font.FontColor = XLColor.White;
            wsVentas.Range("A1:J1").Style.Fill.BackgroundColor = XLColor.FromArgb(30, 58, 138); // Dark Blue
            wsVentas.Range("A1:J1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] headersVentas = {
                "No.", "Fecha y Hora", "Tipo Comprobante", "No. Comprobante",
                "Cliente", "DUI / NIT / NRC", "Método de Pago",
                "Venta Gravada ($)", "Débito IVA 13% ($)", "Total Facturado ($)"
            };

            for (int i = 0; i < headersVentas.Length; i++)
            {
                var cell = wsVentas.Cell(3, i + 1);
                cell.Value = headersVentas[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(37, 99, 235);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int filaV = 4;
            int numItemV = 1;
            foreach (var v in ventas)
            {
                wsVentas.Cell(filaV, 1).Value = numItemV++;
                wsVentas.Cell(filaV, 2).Value = v.FechaHora.ToString("dd/MM/yyyy HH:mm");
                wsVentas.Cell(filaV, 3).Value = v.TipoComprobante;
                wsVentas.Cell(filaV, 4).Value = v.NumComprobante;
                wsVentas.Cell(filaV, 5).Value = v.Cliente?.Nombre ?? "Consumidor Final";
                wsVentas.Cell(filaV, 6).Value = !string.IsNullOrEmpty(v.Cliente?.Nrc) ? v.Cliente.Nrc : (v.Cliente?.NumDocumento ?? "N/A");
                wsVentas.Cell(filaV, 7).Value = v.MetodoPago;

                wsVentas.Cell(filaV, 8).Value = v.Subtotal;
                wsVentas.Cell(filaV, 8).Style.NumberFormat.Format = "$#,##0.00";

                wsVentas.Cell(filaV, 9).Value = v.Impuesto;
                wsVentas.Cell(filaV, 9).Style.NumberFormat.Format = "$#,##0.00";

                wsVentas.Cell(filaV, 10).Value = v.Total;
                wsVentas.Cell(filaV, 10).Style.NumberFormat.Format = "$#,##0.00";

                if (filaV % 2 == 0)
                {
                    wsVentas.Range(filaV, 1, filaV, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);
                }

                filaV++;
            }

            // Totales de Ventas
            wsVentas.Range(filaV, 1, filaV, 7).Merge().Value = "TOTALES DEL LIBRO DE VENTAS:";
            wsVentas.Range(filaV, 1, filaV, 7).Style.Font.Bold = true;
            wsVentas.Range(filaV, 1, filaV, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            wsVentas.Cell(filaV, 8).Value = baseDebitoFiscal;
            wsVentas.Cell(filaV, 8).Style.NumberFormat.Format = "$#,##0.00";
            wsVentas.Cell(filaV, 8).Style.Font.Bold = true;

            wsVentas.Cell(filaV, 9).Value = debitoFiscal;
            wsVentas.Cell(filaV, 9).Style.NumberFormat.Format = "$#,##0.00";
            wsVentas.Cell(filaV, 9).Style.Font.Bold = true;

            wsVentas.Cell(filaV, 10).Value = totalVentas;
            wsVentas.Cell(filaV, 10).Style.NumberFormat.Format = "$#,##0.00";
            wsVentas.Cell(filaV, 10).Style.Font.Bold = true;

            wsVentas.Range(filaV, 1, filaV, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(224, 231, 255);
            wsVentas.Range(3, 1, filaV, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsVentas.Range(3, 1, filaV, 10).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            wsVentas.Columns().AdjustToContents();

            // ==========================================
            // HOJA 3: DETALLE LIBRO DE COMPRAS (CRÉDITO FISCAL)
            // ==========================================
            var wsCompras = workbook.Worksheets.Add("Libro Compras (Crédito)");
            wsCompras.ShowGridLines = true;

            wsCompras.Range("A1:I1").Merge().Value = "LIBRO DE COMPRAS A PROVEEDORES (CRÉDITO FISCAL)";
            wsCompras.Range("A1:I1").Style.Font.Bold = true;
            wsCompras.Range("A1:I1").Style.Font.FontSize = 14;
            wsCompras.Range("A1:I1").Style.Font.FontColor = XLColor.White;
            wsCompras.Range("A1:I1").Style.Fill.BackgroundColor = XLColor.FromArgb(20, 83, 45); // Dark Green
            wsCompras.Range("A1:I1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] headersCompras = {
                "No.", "Fecha y Hora", "No. Factura Proveedor", "Proveedor",
                "NRC", "NIT", "Compra Gravada ($)", "Crédito IVA 13% ($)", "Total Compra ($)"
            };

            for (int i = 0; i < headersCompras.Length; i++)
            {
                var cell = wsCompras.Cell(3, i + 1);
                cell.Value = headersCompras[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(22, 163, 74);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int filaC = 4;
            int numItemC = 1;
            foreach (var c in compras)
            {
                var (baseComp, ivaComp) = c.Total > 0 ? FinancialRoundingHelper.DesglosarIvaIncluido(c.Total) : (0.00m, 0.00m);

                wsCompras.Cell(filaC, 1).Value = numItemC++;
                wsCompras.Cell(filaC, 2).Value = c.FechaHora.ToString("dd/MM/yyyy HH:mm");
                wsCompras.Cell(filaC, 3).Value = c.NumFactura;
                wsCompras.Cell(filaC, 4).Value = c.Proveedor?.Nombre ?? "Proveedor General";
                wsCompras.Cell(filaC, 5).Value = c.Proveedor?.Nrc ?? "N/A";
                wsCompras.Cell(filaC, 6).Value = c.Proveedor?.Nit ?? "N/A";

                wsCompras.Cell(filaC, 7).Value = baseComp;
                wsCompras.Cell(filaC, 7).Style.NumberFormat.Format = "$#,##0.00";

                wsCompras.Cell(filaC, 8).Value = ivaComp;
                wsCompras.Cell(filaC, 8).Style.NumberFormat.Format = "$#,##0.00";

                wsCompras.Cell(filaC, 9).Value = c.Total;
                wsCompras.Cell(filaC, 9).Style.NumberFormat.Format = "$#,##0.00";

                if (filaC % 2 == 0)
                {
                    wsCompras.Range(filaC, 1, filaC, 9).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 253, 244);
                }

                filaC++;
            }

            // Totales de Compras
            wsCompras.Range(filaC, 1, filaC, 6).Merge().Value = "TOTALES DEL LIBRO DE COMPRAS:";
            wsCompras.Range(filaC, 1, filaC, 6).Style.Font.Bold = true;
            wsCompras.Range(filaC, 1, filaC, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            wsCompras.Cell(filaC, 7).Value = baseCreditoFiscal;
            wsCompras.Cell(filaC, 7).Style.NumberFormat.Format = "$#,##0.00";
            wsCompras.Cell(filaC, 7).Style.Font.Bold = true;

            wsCompras.Cell(filaC, 8).Value = creditoFiscal;
            wsCompras.Cell(filaC, 8).Style.NumberFormat.Format = "$#,##0.00";
            wsCompras.Cell(filaC, 8).Style.Font.Bold = true;

            wsCompras.Cell(filaC, 9).Value = totalCompras;
            wsCompras.Cell(filaC, 9).Style.NumberFormat.Format = "$#,##0.00";
            wsCompras.Cell(filaC, 9).Style.Font.Bold = true;

            wsCompras.Range(filaC, 1, filaC, 9).Style.Fill.BackgroundColor = XLColor.FromArgb(220, 252, 231);
            wsCompras.Range(3, 1, filaC, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsCompras.Range(3, 1, filaC, 9).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            wsCompras.Columns().AdjustToContents();

            // Generar bytes
            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            byte[] bytes = ms.ToArray();

            string nombreArchivo = $"Libros_IVA_{periodo}_{ahora:yyyyMMdd_HHmmss}.xlsx";
            string rutaLocal = GuardarEnDescargas(nombreArchivo, bytes);

            return new ExcelExportResult
            {
                Exitoso = true,
                Mensaje = $"Libros de IVA exportados con éxito ({ventas.Count} ventas y {compras.Count} compras registradas).",
                NombreArchivo = nombreArchivo,
                RutaArchivoLocal = rutaLocal,
                ContenidoBytes = bytes
            };
        }
        catch (Exception ex)
        {
            return new ExcelExportResult
            {
                Exitoso = false,
                Mensaje = $"Error al generar Excel de Libros de IVA: {ex.Message}"
            };
        }
    }

    public async Task<ExcelExportResult> ExportarAlertasStockBajoAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            DateTime ahora = DateTime.Now;

            // Consultar productos reales con stock bajo
            var productos = await context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Estado && p.Stock <= p.StockMinimo)
                .OrderBy(p => p.Stock)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Alertas de Stock Bajo");
            ws.ShowGridLines = true;

            // Encabezado
            ws.Range("A1:K1").Merge().Value = "COREAS ERP - REPORTE DE ALERTAS DE INVENTARIO CON STOCK BAJO";
            ws.Range("A1:K1").Style.Font.Bold = true;
            ws.Range("A1:K1").Style.Font.FontSize = 14;
            ws.Range("A1:K1").Style.Font.FontColor = XLColor.White;
            ws.Range("A1:K1").Style.Fill.BackgroundColor = XLColor.FromArgb(153, 27, 27); // Dark Red
            ws.Range("A1:K1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range("A2:K2").Merge().Value = $"Generado el: {ahora:dd/MM/yyyy HH:mm:ss} | Total Artículos Críticos: {productos.Count}";
            ws.Range("A2:K2").Style.Font.Italic = true;
            ws.Range("A2:K2").Style.Fill.BackgroundColor = XLColor.FromArgb(254, 242, 242);
            ws.Range("A2:K2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] headers = {
                "No.", "Código de Barra", "Descripción del Producto", "Categoría",
                "Stock Actual", "Stock Mínimo", "Déficit a Reordenar",
                "Costo Compra ($)", "Precio Venta ($)", "Inversión Estimada ($)", "Estado"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(185, 28, 28);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int fila = 5;
            int numItem = 1;
            decimal totalInversionEstimada = 0.00m;
            decimal totalUnidadesFaltantes = 0m;

            foreach (var p in productos)
            {
                decimal deficit = Math.Max(0m, p.StockMinimo - p.Stock);
                decimal inversion = deficit * p.PrecioCompra;

                totalUnidadesFaltantes += deficit;
                totalInversionEstimada += inversion;

                ws.Cell(fila, 1).Value = numItem++;
                ws.Cell(fila, 2).Value = p.Codigo;
                ws.Cell(fila, 3).Value = p.Nombre;
                ws.Cell(fila, 4).Value = p.Categoria?.Nombre ?? "Sin Categoría";
                ws.Cell(fila, 5).Value = p.Stock;
                ws.Cell(fila, 6).Value = p.StockMinimo;
                ws.Cell(fila, 7).Value = deficit;

                ws.Cell(fila, 8).Value = p.PrecioCompra;
                ws.Cell(fila, 8).Style.NumberFormat.Format = "$#,##0.00";

                ws.Cell(fila, 9).Value = p.PrecioVenta;
                ws.Cell(fila, 9).Style.NumberFormat.Format = "$#,##0.00";

                ws.Cell(fila, 10).Value = inversion;
                ws.Cell(fila, 10).Style.NumberFormat.Format = "$#,##0.00";

                string estadoTexto = p.Stock == 0 ? "AGOTADO" : "REORDENAR";
                var cellEstado = ws.Cell(fila, 11);
                cellEstado.Value = estadoTexto;
                cellEstado.Style.Font.Bold = true;
                cellEstado.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (p.Stock == 0)
                {
                    cellEstado.Style.Font.FontColor = XLColor.FromArgb(153, 27, 27);
                    cellEstado.Style.Fill.BackgroundColor = XLColor.FromArgb(254, 226, 226);
                }
                else
                {
                    cellEstado.Style.Font.FontColor = XLColor.FromArgb(180, 83, 9);
                    cellEstado.Style.Fill.BackgroundColor = XLColor.FromArgb(254, 243, 199);
                }

                if (fila % 2 == 0 && p.Stock > 0)
                {
                    ws.Range(fila, 1, fila, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(255, 251, 235);
                }

                fila++;
            }

            // Totales
            ws.Range(fila, 1, fila, 4).Merge().Value = "TOTALES DE REABASTECIMIENTO:";
            ws.Range(fila, 1, fila, 4).Style.Font.Bold = true;
            ws.Range(fila, 1, fila, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(fila, 5).Value = productos.Sum(x => x.Stock);
            ws.Cell(fila, 5).Style.Font.Bold = true;

            ws.Cell(fila, 6).Value = productos.Sum(x => x.StockMinimo);
            ws.Cell(fila, 6).Style.Font.Bold = true;

            ws.Cell(fila, 7).Value = totalUnidadesFaltantes;
            ws.Cell(fila, 7).Style.Font.Bold = true;

            ws.Range(fila, 8, fila, 9).Merge().Value = "-";
            ws.Range(fila, 8, fila, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(fila, 10).Value = totalInversionEstimada;
            ws.Cell(fila, 10).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(fila, 10).Style.Font.Bold = true;

            ws.Cell(fila, 11).Value = $"{productos.Count} alertas";
            ws.Cell(fila, 11).Style.Font.Bold = true;
            ws.Cell(fila, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(fila, 1, fila, 11).Style.Fill.BackgroundColor = XLColor.FromArgb(254, 226, 226);
            ws.Range(4, 1, fila, 11).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(4, 1, fila, 11).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Columns().AdjustToContents();

            // Generar bytes
            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            byte[] bytes = ms.ToArray();

            string nombreArchivo = $"Alertas_Stock_Bajo_{ahora:yyyyMMdd_HHmmss}.xlsx";
            string rutaLocal = GuardarEnDescargas(nombreArchivo, bytes);

            return new ExcelExportResult
            {
                Exitoso = true,
                Mensaje = $"Reporte de alertas de stock bajo exportado con éxito ({productos.Count} artículos críticos).",
                NombreArchivo = nombreArchivo,
                RutaArchivoLocal = rutaLocal,
                ContenidoBytes = bytes
            };
        }
        catch (Exception ex)
        {
            return new ExcelExportResult
            {
                Exitoso = false,
                Mensaje = $"Error al generar Excel de alertas de stock bajo: {ex.Message}"
            };
        }
    }

    public async Task<ExcelExportResult> ExportarInventarioCompletoAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            DateTime ahora = DateTime.Now;

            var productos = await context.Productos
                .Include(p => p.Categoria)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Inventario de Productos");
            ws.ShowGridLines = true;

            // Encabezado
            ws.Range("A1:J1").Merge().Value = "COREAS ERP - CATÁLOGO E INVENTARIO GENERAL DE PRODUCTOS";
            ws.Range("A1:J1").Style.Font.Bold = true;
            ws.Range("A1:J1").Style.Font.FontSize = 14;
            ws.Range("A1:J1").Style.Font.FontColor = XLColor.White;
            ws.Range("A1:J1").Style.Fill.BackgroundColor = XLColor.FromArgb(16, 185, 129); // Emerald
            ws.Range("A1:J1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range("A2:J2").Merge().Value = $"Fecha de Emisión: {ahora:dd/MM/yyyy HH:mm:ss} | Total Artículos Registrados: {productos.Count}";
            ws.Range("A2:J2").Style.Font.Italic = true;
            ws.Range("A2:J2").Style.Fill.BackgroundColor = XLColor.FromArgb(236, 253, 245);
            ws.Range("A2:J2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] headers = {
                "#", "Código / SKU", "Nombre del Producto", "Categoría",
                "Costo Compra ($)", "Precio Venta ($)", "Stock Actual", "Stock Mínimo",
                "Margen Excepción (%)", "Estado"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(5, 150, 105);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int fila = 5;
            int numItem = 1;
            foreach (var p in productos)
            {
                ws.Cell(fila, 1).Value = numItem++;
                ws.Cell(fila, 2).Value = p.Codigo;
                ws.Cell(fila, 3).Value = p.Nombre;
                ws.Cell(fila, 4).Value = p.Categoria?.Nombre ?? "Sin Categoría";

                ws.Cell(fila, 5).Value = p.PrecioCompra;
                ws.Cell(fila, 5).Style.NumberFormat.Format = "$#,##0.00";

                ws.Cell(fila, 6).Value = p.PrecioVenta;
                ws.Cell(fila, 6).Style.NumberFormat.Format = "$#,##0.00";

                ws.Cell(fila, 7).Value = p.Stock;
                ws.Cell(fila, 8).Value = p.StockMinimo;

                ws.Cell(fila, 9).Value = $"{p.MargenExcepcion:0.00}%";
                ws.Cell(fila, 10).Value = p.Estado ? "Activo" : "Inactivo";

                if (p.Stock <= p.StockMinimo)
                {
                    ws.Cell(fila, 7).Style.Fill.BackgroundColor = p.Stock == 0 ? XLColor.FromArgb(254, 226, 226) : XLColor.FromArgb(254, 243, 199);
                    ws.Cell(fila, 7).Style.Font.Bold = true;
                }

                if (fila % 2 == 0)
                {
                    ws.Range(fila, 1, fila, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(249, 250, 251);
                }

                fila++;
            }

            // Fila de totales
            ws.Range(fila, 1, fila, 6).Merge().Value = "TOTAL PRODUCTOS & EXISTENCIAS TOTALES:";
            ws.Range(fila, 1, fila, 6).Style.Font.Bold = true;
            ws.Range(fila, 1, fila, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(fila, 7).Value = productos.Sum(p => p.Stock);
            ws.Cell(fila, 7).Style.Font.Bold = true;

            ws.Range(fila, 8, fila, 9).Merge().Value = "-";
            ws.Range(fila, 8, fila, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(fila, 10).Value = $"{productos.Count(p => p.Estado)} Activos";
            ws.Cell(fila, 10).Style.Font.Bold = true;
            ws.Cell(fila, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(fila, 1, fila, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(209, 250, 229);
            ws.Range(4, 1, fila, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(4, 1, fila, 10).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            byte[] bytes = ms.ToArray();

            string nombreArchivo = $"Inventario_Productos_{ahora:yyyyMMdd_HHmmss}.xlsx";
            string rutaLocal = GuardarEnDescargas(nombreArchivo, bytes);

            return new ExcelExportResult
            {
                Exitoso = true,
                Mensaje = $"Inventario exportado con éxito ({productos.Count} productos procesados).",
                NombreArchivo = nombreArchivo,
                RutaArchivoLocal = rutaLocal,
                ContenidoBytes = bytes
            };
        }
        catch (Exception ex)
        {
            return new ExcelExportResult
            {
                Exitoso = false,
                Mensaje = $"Error al exportar inventario: {ex.Message}"
            };
        }
    }

    public async Task<ExcelImportResult> ImportarInventarioExcelAsync(Stream fileStream)
    {
        var result = new ExcelImportResult();
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms);
            ms.Position = 0;

            if (ms.Length < 4)
            {
                result.Exitoso = false;
                result.Mensaje = "El archivo seleccionado está vacío o no contiene información.";
                return result;
            }

            // Detección de formato: ¿Es un archivo Excel OOXML (.xlsx) con cabecera ZIP 'PK' (0x50, 0x4B)?
            byte b0 = (byte)ms.ReadByte();
            byte b1 = (byte)ms.ReadByte();
            ms.Position = 0;

            List<List<string>> matriz;

            bool esXlsx = (b0 == 0x50 && b1 == 0x4B);
            if (esXlsx)
            {
                try
                {
                    matriz = ExtraerMatrizDesdeXlsx(ms);
                }
                catch
                {
                    // Fallback: Si ClosedXML falla al abrirlo como xlsx, intentar interpretarlo como CSV/Texto
                    ms.Position = 0;
                    matriz = ParsearCsv(ms);
                }
            }
            else
            {
                // Es un archivo de texto: CSV delimitado por comas, punto y coma o tabulaciones
                matriz = ParsearCsv(ms);
            }

            if (matriz.Count == 0)
            {
                result.Exitoso = false;
                result.Mensaje = "No se encontraron datos legibles en el archivo. Asegúrate de que no esté vacío.";
                return result;
            }

            return await ProcesarMatrizImportacionAsync(matriz, context, result);
        }
        catch (Exception ex)
        {
            result.Exitoso = false;
            result.Mensaje = $"Aviso al procesar archivo: {ex.Message}. Asegúrate de usar la plantilla oficial de Excel o un archivo CSV delimitado por comas o punto y coma.";
            return result;
        }
    }

    private static List<List<string>> ExtraerMatrizDesdeXlsx(Stream stream)
    {
        var matriz = new List<List<string>>();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.FirstOrDefault();
        if (ws == null) return matriz;

        int lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 12;
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        for (int r = 1; r <= lastRow; r++)
        {
            var fila = new List<string>();
            bool filaTieneDatos = false;
            for (int c = 1; c <= lastCol; c++)
            {
                string val = ws.Cell(r, c).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(val)) filaTieneDatos = true;
                fila.Add(val);
            }
            if (filaTieneDatos)
            {
                matriz.Add(fila);
            }
        }
        return matriz;
    }

    private static List<List<string>> ParsearCsv(Stream stream)
    {
        var matriz = new List<List<string>>();
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        string? firstLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(firstLine)) return matriz;

        // Auto-detectar delimitador: ',' o ';' o '\t'
        char delimiter = ',';
        int commas = firstLine.Count(c => c == ',');
        int semicolons = firstLine.Count(c => c == ';');
        int tabs = firstLine.Count(c => c == '\t');

        if (semicolons > commas && semicolons >= tabs)
        {
            delimiter = ';';
        }
        else if (tabs > commas && tabs > semicolons)
        {
            delimiter = '\t';
        }

        matriz.Add(ParsearLineaCsv(firstLine, delimiter));

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var celdas = ParsearLineaCsv(line, delimiter);
            if (celdas.Any(c => !string.IsNullOrWhiteSpace(c)))
            {
                matriz.Add(celdas);
            }
        }

        return matriz;
    }

    private static List<string> ParsearLineaCsv(string line, char delimiter)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        result.Add(sb.ToString().Trim());
        return result;
    }

    private static decimal ParsearDecimalSeguro(string? raw, decimal defaultValue = 0m)
    {
        if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
        string s = raw.Replace("$", "").Replace("%", "").Trim();
        if (string.IsNullOrWhiteSpace(s)) return defaultValue;

        if (s.Contains(",") && s.Contains("."))
        {
            if (s.LastIndexOf('.') > s.LastIndexOf(','))
            {
                s = s.Replace(",", "");
            }
            else
            {
                s = s.Replace(".", "").Replace(",", ".");
            }
        }
        else if (s.Contains(","))
        {
            s = s.Replace(",", ".");
        }

        if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
        {
            return val;
        }
        return defaultValue;
    }

    private async Task<ExcelImportResult> ProcesarMatrizImportacionAsync(List<List<string>> matriz, AppDbContext context, ExcelImportResult result)
    {
        var categoriasExistentes = await context.Categorias.ToListAsync();
        var dictCategorias = categoriasExistentes.ToDictionary(c => c.Nombre.Trim().ToLower(), c => c);

        int startRow = 1;
        int colCodigo = -1;
        int colNombre = -1;
        int colCategoria = -1;
        int colCosto = -1;
        int colPrecioVenta = -1;
        int colPrecioGranel = -1;
        int colStock = -1;
        int colStockMinimo = -1;
        int colMargenExcepcion = -1;
        int colPresentacion = -1;

        // Escanear las primeras 6 filas para detectar encabezados dinámicamente
        int scanRows = Math.Min(6, matriz.Count);
        for (int r = 0; r < scanRows; r++)
        {
            var fila = matriz[r];
            int matchedHeaders = 0;

            for (int c = 0; c < fila.Count; c++)
            {
                string h = fila[c].ToLower();
                if (string.IsNullOrWhiteSpace(h)) continue;

                if (h.Contains("código") || h.Contains("codigo") || h.Contains("sku") || h.Contains("upc") || h.Contains("barras") || h.Contains("code"))
                {
                    matchedHeaders++;
                }
                else if (h.Contains("descripción") || h.Contains("descripcion") || h.Contains("nombre") || (h.Contains("producto") && !h.Contains("upc")))
                {
                    matchedHeaders++;
                }
            }

            if (matchedHeaders >= 1)
            {
                startRow = r + 1; // Las filas de datos empiezan después de este encabezado
                for (int c = 0; c < fila.Count; c++)
                {
                    string h = fila[c].ToLower();
                    if (string.IsNullOrWhiteSpace(h)) continue;

                    if ((h.Contains("código") || h.Contains("codigo") || h.Contains("sku") || h.Contains("upc") || h.Contains("barras") || h.Contains("code")) && colCodigo == -1)
                    {
                        colCodigo = c;
                    }
                    else if ((h.Contains("descripción") || h.Contains("descripcion") || h.Contains("nombre") || h.Contains("producto")) && colNombre == -1)
                    {
                        colNombre = c;
                    }
                    else if ((h.Contains("categoría") || h.Contains("categoria") || h.Contains("rubro")) && colCategoria == -1)
                    {
                        colCategoria = c;
                    }
                    else if ((h.Contains("granel") || h.Contains("caja") || h.Contains("fardo")) && colPrecioGranel == -1)
                    {
                        colPrecioGranel = c;
                    }
                    else if ((h.Contains("venta") || h.Contains("vta") || h.Contains("precio v")) && colPrecioVenta == -1)
                    {
                        colPrecioVenta = c;
                    }
                    else if ((h.Contains("costo") || h.Contains("compra") || h.Contains("unitario") || h.Contains("precio u")) && colCosto == -1)
                    {
                        colCosto = c;
                    }
                    else if ((h.Contains("stock") || h.Contains("cantidad") || h.Contains("cant") || h.Contains("existencia")) && colStock == -1)
                    {
                        colStock = c;
                    }
                    else if ((h.Contains("mínimo") || h.Contains("minimo") || h.Contains("alerta")) && colStockMinimo == -1)
                    {
                        colStockMinimo = c;
                    }
                    else if ((h.Contains("margen") || h.Contains("excep")) && colMargenExcepcion == -1)
                    {
                        colMargenExcepcion = c;
                    }
                    else if ((h.Contains("presentación") || h.Contains("presentacion") || h.Contains("unidad") || h.Contains("empaque")) && colPresentacion == -1)
                    {
                        colPresentacion = c;
                    }
                }
                break;
            }
        }

        // Fallbacks por posición si no se detectó por texto de encabezado
        if (colCodigo == -1) colCodigo = 1;
        if (colNombre == -1) colNombre = 2;
        if (colCategoria == -1) colCategoria = 3;
        if (colCosto == -1) colCosto = 4;
        if (colPrecioVenta == -1) colPrecioVenta = 5;
        if (colStock == -1) colStock = 6;
        if (colStockMinimo == -1) colStockMinimo = 7;
        if (colMargenExcepcion == -1) colMargenExcepcion = 8;
        if (colPresentacion == -1) colPresentacion = 9;

        if (matriz.Count <= startRow)
        {
            result.Exitoso = false;
            result.Mensaje = "No se encontraron filas de productos para importar debajo de los encabezados.";
            return result;
        }

        for (int r = startRow; r < matriz.Count; r++)
        {
            var fila = matriz[r];
            if (fila.Count == 0) continue;

            string GetCol(int idx) => (idx >= 0 && idx < fila.Count) ? fila[idx].Trim() : "";

            string codigo = GetCol(colCodigo);
            if (string.IsNullOrWhiteSpace(codigo) || codigo.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string nombre = GetCol(colNombre);
            if (string.IsNullOrWhiteSpace(nombre))
            {
                continue;
            }

            string categoriaNombre = colCategoria >= 0 ? GetCol(colCategoria) : "General";
            if (string.IsNullOrWhiteSpace(categoriaNombre)) categoriaNombre = "General";

            string presentacion = colPresentacion >= 0 ? GetCol(colPresentacion) : "Unidad";
            if (string.IsNullOrWhiteSpace(presentacion)) presentacion = "Unidad";

            decimal costo = FinancialRoundingHelper.Round4(ParsearDecimalSeguro(GetCol(colCosto), 0.00m));
            decimal precioVenta = FinancialRoundingHelper.Round4(ParsearDecimalSeguro(GetCol(colPrecioVenta), 0.00m));
            decimal? precioGranel = null;
            if (colPrecioGranel >= 0)
            {
                decimal pg = FinancialRoundingHelper.Round4(ParsearDecimalSeguro(GetCol(colPrecioGranel), 0.00m));
                if (pg > 0) precioGranel = pg;
            }

            decimal stock = FinancialRoundingHelper.Round3(ParsearDecimalSeguro(GetCol(colStock), 0m));
            decimal stockMinimo = FinancialRoundingHelper.Round3(ParsearDecimalSeguro(GetCol(colStockMinimo), 5m));
            decimal margenExcepcion = FinancialRoundingHelper.Round2(ParsearDecimalSeguro(GetCol(colMargenExcepcion), 0.00m));

            result.TotalProcesados++;

            // Buscar o crear categoría
            string keyCat = categoriaNombre.ToLower();
            if (!dictCategorias.TryGetValue(keyCat, out var cat))
            {
                cat = new Categoria
                {
                    Nombre = categoriaNombre,
                    Descripcion = $"Categoría {categoriaNombre}",
                    Icono = "fa-box",
                    Estado = true
                };
                await context.Categorias.AddAsync(cat);
                await context.SaveChangesAsync();
                dictCategorias[keyCat] = cat;
            }

            decimal cantEmpaque = 1.00m;
            if (decimal.TryParse(presentacion, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pNum) && pNum > 1)
            {
                cantEmpaque = pNum;
            }
            else
            {
                var match = System.Text.RegularExpressions.Regex.Match(nombre, @"\b(\d+)\s*[xX*]\s*(\d+)\b");
                if (match.Success && decimal.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cantFromDesc) && cantFromDesc > 1)
                {
                    cantEmpaque = cantFromDesc;
                }
            }

            decimal? costoGranel = null;
            if (cantEmpaque > 1 && costo > 0)
            {
                costoGranel = FinancialRoundingHelper.Round4(costo / cantEmpaque);
            }

            // Si el precio de venta es menor al costo del empaque (ej: 0.12 vs 4.57), es el precio de venta unitario/granel
            if (cantEmpaque > 1 && precioVenta > 0 && costo > 0 && precioVenta < costo)
            {
                if (!precioGranel.HasValue || precioGranel.Value <= 0)
                {
                    precioGranel = precioVenta;
                }
                precioVenta = FinancialRoundingHelper.Round4(precioVenta * cantEmpaque);
            }

            bool esGranel = (precioGranel.HasValue && precioGranel.Value > 0) || cantEmpaque > 1;

            // Buscar si el producto ya existe por código
            var prod = await context.Productos.FirstOrDefaultAsync(p => p.Codigo == codigo);
            if (prod != null)
            {
                prod.Nombre = nombre;
                prod.IdCategoria = cat.IdCategoria;
                if (costo > 0) prod.PrecioCompra = costo;
                if (precioVenta > 0) prod.PrecioVenta = precioVenta;
                if (precioGranel.HasValue && precioGranel.Value > 0)
                {
                    prod.PrecioGranel = precioGranel.Value;
                }
                if (esGranel)
                {
                    prod.EsGranel = true;
                }
                if (cantEmpaque > 1)
                {
                    prod.CantidadPorEmpaque = cantEmpaque;
                    prod.CostoGranel = costoGranel;
                }
                if (stock >= 0) prod.Stock = stock;
                if (stockMinimo >= 0) prod.StockMinimo = stockMinimo;
                if (margenExcepcion >= 0) prod.MargenExcepcion = margenExcepcion;
                if (!string.IsNullOrWhiteSpace(presentacion) && presentacion != "1" && presentacion != "12")
                {
                    prod.UnidadMedida = presentacion;
                }
                prod.Estado = true;
                result.TotalActualizados++;
            }
            else
            {
                prod = new Producto
                {
                    Codigo = codigo,
                    Nombre = nombre,
                    IdCategoria = cat.IdCategoria,
                    PrecioCompra = costo,
                    PrecioVenta = precioVenta,
                    PrecioGranel = precioGranel,
                    CostoGranel = cantEmpaque > 1 ? costoGranel : null,
                    CantidadPorEmpaque = cantEmpaque > 1 ? cantEmpaque : 1.00m,
                    EsGranel = esGranel,
                    UnidadMedida = (!string.IsNullOrWhiteSpace(presentacion) && presentacion != "1" && presentacion != "12") ? presentacion : "Unidad",
                    Stock = stock,
                    StockMinimo = stockMinimo > 0 ? stockMinimo : 5,
                    MargenExcepcion = margenExcepcion,
                    Estado = true,
                    FechaCreacion = DateTime.Now
                };
                await context.Productos.AddAsync(prod);
                result.TotalInsertados++;
            }
        }

        await context.SaveChangesAsync();
        result.Exitoso = true;
        result.Mensaje = $"¡Importación exitosa! {result.TotalProcesados} productos procesados ({result.TotalInsertados} nuevos creados, {result.TotalActualizados} actualizados).";
        return result;
    }

    public byte[] GenerarPlantillaImportacionExcel()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Plantilla_Inventario");
        ws.ShowGridLines = true;

        string[] headers = {
            "#", "Código / SKU", "Nombre del Producto", "Categoría",
            "Costo Compra ($)", "Precio Venta ($)", "Stock Actual", "Stock Mínimo",
            "Margen Excepción (%)", "Presentación / Empaque"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(20, 83, 45); // Verde Coreas ERP
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Fila de ejemplo 1: Producto estándar
        ws.Cell(2, 1).Value = 1;
        ws.Cell(2, 2).Value = "ART-001";
        ws.Cell(2, 3).Value = "Arroz Blanco 1lb";
        ws.Cell(2, 4).Value = "Granos Básicos";
        ws.Cell(2, 5).Value = 0.65m;
        ws.Cell(2, 6).Value = 0.85m;
        ws.Cell(2, 7).Value = 50;
        ws.Cell(2, 8).Value = 10;
        ws.Cell(2, 9).Value = 0.00m;
        ws.Cell(2, 10).Value = "Unidad";

        // Fila de ejemplo 2: Producto con empaque / oferta (ej. jabón 3x1)
        ws.Cell(3, 1).Value = 2;
        ws.Cell(3, 2).Value = "JAB-002";
        ws.Cell(3, 3).Value = "Jabón en Barra (Pack 3x1)";
        ws.Cell(3, 4).Value = "Limpieza";
        ws.Cell(3, 5).Value = 3.03m;
        ws.Cell(3, 6).Value = 3.60m;
        ws.Cell(3, 7).Value = 30;
        ws.Cell(3, 8).Value = 5;
        ws.Cell(3, 9).Value = 0.00m;
        ws.Cell(3, 10).Value = "3x1";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public ExcelExportResult DescargarPlantillaImportacion()
    {
        try
        {
            byte[] bytes = GenerarPlantillaImportacionExcel();
            string nombreArchivo = "Plantilla_Inventario_CoreasERP.xlsx";
            string rutaLocal = GuardarEnDescargas(nombreArchivo, bytes);

            return new ExcelExportResult
            {
                Exitoso = true,
                Mensaje = $"Plantilla guardada exitosamente en tu carpeta de Descargas.",
                NombreArchivo = nombreArchivo,
                RutaArchivoLocal = rutaLocal,
                ContenidoBytes = bytes
            };
        }
        catch (Exception ex)
        {
            return new ExcelExportResult
            {
                Exitoso = false,
                Mensaje = $"Error al generar plantilla: {ex.Message}"
            };
        }
    }

    private string GuardarEnDescargas(string nombreArchivo, byte[] bytes)
    {
        try
        {
            string userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (!Directory.Exists(userDownloads))
            {
                Directory.CreateDirectory(userDownloads);
            }
            string fullPath = Path.Combine(userDownloads, nombreArchivo);
            File.WriteAllBytes(fullPath, bytes);
            return fullPath;
        }
        catch
        {
            string fallbackDir = Path.GetTempPath();
            string fallbackPath = Path.Combine(fallbackDir, nombreArchivo);
            File.WriteAllBytes(fallbackPath, bytes);
            return fallbackPath;
        }
    }

    public bool AbrirArchivo(string rutaArchivo)
    {
        try
        {
            if (File.Exists(rutaArchivo))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = rutaArchivo,
                    UseShellExecute = true
                });
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool AbrirCarpeta(string rutaArchivo)
    {
        try
        {
            if (File.Exists(rutaArchivo))
            {
                Process.Start("explorer.exe", $"/select,\"{rutaArchivo}\"");
                return true;
            }
            else
            {
                string dir = Path.GetDirectoryName(rutaArchivo) ?? "";
                if (Directory.Exists(dir))
                {
                    Process.Start("explorer.exe", dir);
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
