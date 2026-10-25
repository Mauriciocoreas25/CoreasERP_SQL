using Microsoft.EntityFrameworkCore;
using CoreasERP.Data;
using CoreasERP.Models;

namespace CoreasERP.Services;

public class TopProductoDto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal CantidadVendida { get; set; }
    public decimal TotalFacturado { get; set; }
}

public class LibroIvaDto
{
    public decimal TotalVentas { get; set; }
    public decimal BaseDebitoFiscal { get; set; }
    public decimal DebitoFiscal { get; set; } // IVA 13% de ventas

    public decimal TotalCompras { get; set; }
    public decimal BaseCreditoFiscal { get; set; }
    public decimal CreditoFiscal { get; set; } // IVA 13% de compras

    public decimal BalanceIva => DebitoFiscal - CreditoFiscal;
    public int ConteoComprobantesVenta { get; set; }
    public int ConteoCreditosFiscalesVenta { get; set; }
    public int ConteoCompras { get; set; }
}

public class DashboardFinancieroDto
{
    public string Periodo { get; set; } = "Mes";
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    // Métricas del Período
    public decimal IngresosPeriodo { get; set; }
    public decimal CostoPeriodo { get; set; }
    public decimal UtilidadPeriodo => IngresosPeriodo - CostoPeriodo;
    public decimal MargenUtilidad => IngresosPeriodo > 0 ? Math.Round((UtilidadPeriodo / IngresosPeriodo) * 100m, 1) : 0.0m;
    public int CantidadVentasPeriodo { get; set; }

    // Métricas Históricas Acumuladas
    public decimal IngresosHistorico { get; set; }
    public decimal CostoHistorico { get; set; }
    public decimal UtilidadHistorico => IngresosHistorico - CostoHistorico;
    public int CantidadVentasHistorico { get; set; }

    // Pérdidas por Mermas / Daño / Vencimiento / Consumo Interno (Valuadas a COSTO de Compra)
    public decimal PerdidaMermaPeriodo { get; set; }
    public decimal PerdidaMermaHistorico { get; set; }
    public int ConteoAjustesMermaPeriodo { get; set; }

    public int TotalBajoStock { get; set; }

    public List<Producto> ProductosBajoStock { get; set; } = new();
    public List<TopProductoDto> TopProductos { get; set; } = new();
    public LibroIvaDto LibroIva { get; set; } = new();
}

public interface IDashboardService
{
    Task<DashboardFinancieroDto> GetDashboardFinancieroAsync(string periodo = "Mes", DateTime? fechaDesde = null, DateTime? fechaHasta = null);
}

public class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public DashboardService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<DashboardFinancieroDto> GetDashboardFinancieroAsync(string periodo = "Mes", DateTime? fechaDesde = null, DateTime? fechaHasta = null)
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
                if (fechaDesde.HasValue)
                    fechaInicio = fechaDesde.Value.Date;
                if (fechaHasta.HasValue)
                    fechaFin = fechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                break;
            case "Todos":
            default:
                fechaInicio = null;
                fechaFin = null;
                break;
        }

        // 1. Consultas de Ventas en el período
        var ventasQuery = context.Ventas
            .Where(v => v.Estado == "Completada");

        if (fechaInicio.HasValue)
        {
            ventasQuery = ventasQuery.Where(v => v.FechaHora >= fechaInicio.Value);
        }
        if (fechaFin.HasValue)
        {
            ventasQuery = ventasQuery.Where(v => v.FechaHora <= fechaFin.Value);
        }

        decimal ingresosPeriodo = await ventasQuery.SumAsync(v => (decimal?)v.Total) ?? 0.00m;
        int cantidadVentasPeriodo = await ventasQuery.CountAsync();

        // 2. Alertas de inventario con stock bajo
        var productosBajoStock = await context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.Estado && p.Stock <= p.StockMinimo)
            .OrderBy(p => p.Stock)
            .ToListAsync();

        int totalBajoStock = productosBajoStock.Count;

        // 3. Detalle de ventas para costo y productos más vendidos
        var detalleGlobalQuery = context.DetalleVentas
            .Where(d => d.Venta != null && d.Venta.Estado == "Completada");

        var detallePeriodoQuery = detalleGlobalQuery;
        if (fechaInicio.HasValue)
        {
            detallePeriodoQuery = detallePeriodoQuery.Where(d => d.Venta != null && d.Venta.FechaHora >= fechaInicio.Value);
        }
        if (fechaFin.HasValue)
        {
            detallePeriodoQuery = detallePeriodoQuery.Where(d => d.Venta != null && d.Venta.FechaHora <= fechaFin.Value);
        }

        // Costo de mercadería vendida en el período
        decimal costoPeriodo = await detallePeriodoQuery
            .SumAsync(d => (decimal?)(d.Cantidad * (
                d.Producto == null ? 0m :
                (d.Producto.CostoGranel != null && d.Producto.PrecioGranel != null && d.Precio == d.Producto.PrecioGranel)
                    ? d.Producto.CostoGranel.Value
                    : d.Producto.PrecioCompra
            ))) ?? 0.00m;

        // Totales Históricos Acumulados Globales
        decimal ingresosHistorico = await context.Ventas
            .Where(v => v.Estado == "Completada")
            .SumAsync(v => (decimal?)v.Total) ?? 0.00m;

        decimal costoHistorico = await detalleGlobalQuery
            .SumAsync(d => (decimal?)(d.Cantidad * (
                d.Producto == null ? 0m :
                (d.Producto.CostoGranel != null && d.Producto.PrecioGranel != null && d.Precio == d.Producto.PrecioGranel)
                    ? d.Producto.CostoGranel.Value
                    : d.Producto.PrecioCompra
            ))) ?? 0.00m;

        int cantidadVentasHistorico = await context.Ventas
            .CountAsync(v => v.Estado == "Completada");

        // Top 6 de productos más vendidos en el período
        var topProductos = await detallePeriodoQuery
            .GroupBy(d => new 
            { 
                d.IdProducto, 
                Codigo = d.Producto != null ? d.Producto.Codigo : "", 
                Nombre = d.Producto != null ? d.Producto.Nombre : "", 
                CategoriaNombre = (d.Producto != null && d.Producto.Categoria != null) ? d.Producto.Categoria.Nombre : "Sin Categoría"
            })
            .Select(g => new TopProductoDto
            {
                IdProducto = g.Key.IdProducto,
                Codigo = g.Key.Codigo,
                Nombre = g.Key.Nombre,
                Categoria = g.Key.CategoriaNombre,
                CantidadVendida = g.Sum(x => x.Cantidad),
                TotalFacturado = g.Sum(x => x.Subtotal)
            })
            .OrderByDescending(x => x.CantidadVendida)
            .Take(6)
            .ToListAsync();

        // 4. Libros de IVA: Débito Fiscal (Ventas) y Crédito Fiscal (Compras)
        decimal totalVentas = ingresosPeriodo;
        decimal debitoFiscal = await ventasQuery.SumAsync(v => (decimal?)v.Impuesto) ?? 0.00m;
        decimal baseDebitoFiscal = await ventasQuery.SumAsync(v => (decimal?)v.Subtotal) ?? 0.00m;

        int conteoCreditosFiscalesVenta = await ventasQuery.CountAsync(v => v.TipoComprobante == "Credito Fiscal");

        // Compras en el período
        var comprasQuery = context.Compras
            .Where(c => c.Estado == "Completada");

        if (fechaInicio.HasValue)
        {
            comprasQuery = comprasQuery.Where(c => c.FechaHora >= fechaInicio.Value);
        }
        if (fechaFin.HasValue)
        {
            comprasQuery = comprasQuery.Where(c => c.FechaHora <= fechaFin.Value);
        }

        decimal totalCompras = await comprasQuery.SumAsync(c => (decimal?)c.Total) ?? 0.00m;
        int conteoCompras = await comprasQuery.CountAsync();

        // Base y Crédito Fiscal (13% IVA en Compras)
        var (baseCreditoFiscal, creditoFiscal) = totalCompras > 0 ? FinancialRoundingHelper.DesglosarIvaIncluido(totalCompras) : (0.00m, 0.00m);

        var libroIva = new LibroIvaDto
        {
            TotalVentas = totalVentas,
            BaseDebitoFiscal = baseDebitoFiscal,
            DebitoFiscal = debitoFiscal,
            TotalCompras = totalCompras,
            BaseCreditoFiscal = baseCreditoFiscal,
            CreditoFiscal = creditoFiscal,
            ConteoComprobantesVenta = cantidadVentasPeriodo,
            ConteoCreditosFiscalesVenta = conteoCreditosFiscalesVenta,
            ConteoCompras = conteoCompras
        };

        // 5. Mermas, Averías, Vencimientos y Consumo Interno (Valuados estrictamente a COSTO en tabla dedicada Mermas)
        var mermasQuery = context.Mermas
            .Include(m => m.Producto)
            .AsQueryable();

        var mermasPeriodoQuery = mermasQuery;
        if (fechaInicio.HasValue)
        {
            mermasPeriodoQuery = mermasPeriodoQuery.Where(m => m.FechaHora >= fechaInicio.Value);
        }
        if (fechaFin.HasValue)
        {
            mermasPeriodoQuery = mermasPeriodoQuery.Where(m => m.FechaHora <= fechaFin.Value);
        }

        decimal perdidaMermaPeriodo = await mermasPeriodoQuery
            .SumAsync(m => (decimal?)m.CostoTotal) ?? 0.00m;

        int conteoAjustesMermaPeriodo = await mermasPeriodoQuery.CountAsync();

        decimal perdidaMermaHistorico = await mermasQuery
            .SumAsync(m => (decimal?)m.CostoTotal) ?? 0.00m;

        return new DashboardFinancieroDto
        {
            Periodo = periodo,
            FechaDesde = fechaInicio,
            FechaHasta = fechaFin,
            IngresosPeriodo = ingresosPeriodo,
            CostoPeriodo = costoPeriodo,
            CantidadVentasPeriodo = cantidadVentasPeriodo,
            IngresosHistorico = ingresosHistorico,
            CostoHistorico = costoHistorico,
            CantidadVentasHistorico = cantidadVentasHistorico,
            PerdidaMermaPeriodo = perdidaMermaPeriodo,
            PerdidaMermaHistorico = perdidaMermaHistorico,
            ConteoAjustesMermaPeriodo = conteoAjustesMermaPeriodo,
            TotalBajoStock = totalBajoStock,
            ProductosBajoStock = productosBajoStock,
            TopProductos = topProductos,
            LibroIva = libroIva
        };
    }
}
