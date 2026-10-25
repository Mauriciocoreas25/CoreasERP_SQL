using Microsoft.EntityFrameworkCore;
using CoreasERP.Data;
using CoreasERP.Models;

namespace CoreasERP.Services;

public interface ICashRegisterService
{
    Task<CorteCaja?> GetCorteActivoAsync(int? idUsuario = null);
    Task<(bool Success, string Message, CorteCaja? Corte)> AbrirCajaAsync(int idUsuario, decimal montoInicial);
    Task<(bool Success, string Message, CorteCaja? Corte)> ActualizarMontoInicialAsync(int idCorte, decimal nuevoMontoInicial);
    Task<(bool Success, string Message, CorteCaja? Corte)> CerrarCajaAsync(int idCorte, decimal montoFinal, decimal? diferencia = null);
    Task<(bool Success, string Message, MovimientoCaja? Movimiento)> RegistrarMovimientoCajaAsync(int idUsuario, string tipo, decimal monto, string motivo, int? idCorte = null);
    Task<List<MovimientoCaja>> GetMovimientosCajaAsync(int? idCorte = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null);
    Task<ResumenCorteDto> GenerarResumenCorteAsync(int idCorte, string tipoCorte = "CORTE X", DesgloseDenominacionDto? desgloseFisico = null);
    Task<List<CorteCaja>> GetHistorialCortesAsync();
}

public class CashRegisterService : ICashRegisterService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CashRegisterService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CorteCaja?> GetCorteActivoAsync(int? idUsuario = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.CortesCaja
            .Include(c => c.Usuario)
            .Where(c => c.Estado == "Abierta");

        if (idUsuario.HasValue && idUsuario.Value > 0)
        {
            query = query.Where(c => c.IdUsuario == idUsuario.Value);
        }

        return await query.OrderByDescending(c => c.FechaApertura).FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, CorteCaja? Corte)> AbrirCajaAsync(int idUsuario, decimal montoInicial)
    {
        if (montoInicial < 0)
            return (false, "El fondo inicial no puede ser negativo.", null);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var cajaAbierta = await context.CortesCaja
                .AnyAsync(c => c.IdUsuario == idUsuario && c.Estado == "Abierta");

            if (cajaAbierta)
            {
                return (false, "Ya existe un turno de caja abierto para este usuario.", null);
            }

            var corte = new CorteCaja
            {
                IdUsuario = idUsuario,
                MontoInicial = montoInicial,
                TotalVentas = 0.00m,
                Diferencia = 0.00m,
                FechaApertura = DateTime.Now,
                Estado = "Abierta"
            };

            await context.CortesCaja.AddAsync(corte);
            await context.SaveChangesAsync();
            return (true, "Turno de caja abierto exitosamente.", corte);
        }
        catch (Exception ex)
        {
            return (false, $"Error al abrir turno de caja: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, CorteCaja? Corte)> ActualizarMontoInicialAsync(int idCorte, decimal nuevoMontoInicial)
    {
        if (nuevoMontoInicial < 0)
            return (false, "El fondo inicial no puede ser negativo.", null);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var corte = await context.CortesCaja.FindAsync(idCorte);
            if (corte == null) return (false, "Turno de caja no encontrado.", null);
            if (corte.Estado == "Cerrada") return (false, "No se puede modificar un turno ya cerrado.", null);

            corte.MontoInicial = nuevoMontoInicial;
            await context.SaveChangesAsync();
            return (true, "Fondo inicial actualizado.", corte);
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar fondo inicial: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, MovimientoCaja? Movimiento)> RegistrarMovimientoCajaAsync(
        int idUsuario, string tipo, decimal monto, string motivo, int? idCorte = null)
    {
        if (monto < 0 || (monto == 0 && tipo != "AperturaGaveta"))
            return (false, "El monto debe ser mayor a cero.", null);

        if (string.IsNullOrWhiteSpace(motivo))
            return (false, "Debe especificar el motivo o justificación del movimiento.", null);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (!idCorte.HasValue)
            {
                var corteActivo = await context.CortesCaja
                    .Where(c => c.IdUsuario == idUsuario && c.Estado == "Abierta")
                    .OrderByDescending(c => c.FechaApertura)
                    .FirstOrDefaultAsync();
                if (corteActivo != null)
                {
                    idCorte = corteActivo.IdCorte;
                }
            }

            string motivoFinal = motivo.Trim();
            if (motivoFinal.Length > 250)
            {
                motivoFinal = motivoFinal.Substring(0, 250);
            }

            var mov = new MovimientoCaja
            {
                IdCorte = idCorte,
                IdUsuario = idUsuario,
                Tipo = tipo,
                Monto = monto,
                Motivo = motivoFinal,
                FechaHora = DateTime.Now
            };

            await context.MovimientosCaja.AddAsync(mov);
            await context.SaveChangesAsync();

            string mensajeRespuesta = tipo == "AperturaGaveta"
                ? "Apertura de gaveta registrada en auditoría de caja."
                : $"{tipo} de ${monto:F2} registrado exitosamente.";

            return (true, mensajeRespuesta, mov);
        }
        catch (Exception ex)
        {
            return (false, $"Error al registrar movimiento de caja: {ex.Message}", null);
        }
    }

    public async Task<List<MovimientoCaja>> GetMovimientosCajaAsync(int? idCorte = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.MovimientosCaja.Include(m => m.Usuario).AsQueryable();

        if (idCorte.HasValue)
        {
            query = query.Where(m => m.IdCorte == idCorte.Value);
        }

        if (fechaDesde.HasValue)
        {
            query = query.Where(m => m.FechaHora >= fechaDesde.Value);
        }

        if (fechaHasta.HasValue)
        {
            query = query.Where(m => m.FechaHora <= fechaHasta.Value);
        }

        return await query.OrderByDescending(m => m.FechaHora).ToListAsync();
    }

    public async Task<ResumenCorteDto> GenerarResumenCorteAsync(int idCorte, string tipoCorte = "CORTE X", DesgloseDenominacionDto? desgloseFisico = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var corte = await context.CortesCaja
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.IdCorte == idCorte);

        if (corte == null) return new ResumenCorteDto();

        var config = await context.ConfiguracionNegocio.FirstOrDefaultAsync();

        DateTime fechaInicio = corte.FechaApertura;
        DateTime fechaFin = (corte.Estado == "Cerrada" && corte.FechaCierre.HasValue)
            ? corte.FechaCierre.Value
            : DateTime.Now;

        // Consultar todas las ventas realizadas en el período del turno
        var ventas = await context.Ventas
            .Where(v => v.IdUsuario == corte.IdUsuario && v.FechaHora >= fechaInicio && v.FechaHora <= fechaFin && v.Estado != "Anulada")
            .ToListAsync();

        // Consultar movimientos manuales (gastos, retiros, ingresos)
        var movimientos = await context.MovimientosCaja
            .Where(m => m.IdCorte == idCorte || (m.IdUsuario == corte.IdUsuario && m.FechaHora >= fechaInicio && m.FechaHora <= fechaFin))
            .ToListAsync();

        // Consultar abonos a cuentas por cobrar recibidos en el período (excluyendo anticipos iniciales para no duplicar)
        var abonos = await context.Abonos
            .Where(a => a.IdUsuario == corte.IdUsuario && a.FechaHora >= fechaInicio && a.FechaHora <= fechaFin && (a.Notas == null || !a.Notas.Contains("Anticipo inicial")))
            .ToListAsync();

        var resumen = new ResumenCorteDto
        {
            IdCorte = corte.IdCorte,
            TipoCorte = tipoCorte,
            NombreCajero = corte.Usuario?.Nombre ?? "Cajero",
            NombreEmpresa = config?.NombreNegocio ?? "Coreas ERP",
            Sucursal = config?.CodEstablecimiento ?? "M001",
            PuntoVenta = config?.CodPuntoVenta ?? "P001",
            FechaApertura = corte.FechaApertura,
            FechaCorte = fechaFin,
            EstadoTurno = corte.Estado,
            FondoInicial = corte.MontoInicial,
            Movimientos = movimientos
        };

        // 1. Desglose por Tipo de Comprobante
        var tiposConocidos = new[] { "Ticket", "Factura", "CCF (03)", "Credito Fiscal", "N.Remisión (04)", "N.Crédito (05)", "N.Débito (06)", "Suj.Excluido (14)", "Anticipo", "Cotización" };
        var agrupado = ventas.GroupBy(v => v.TipoComprobante).ToList();

        foreach (var grupo in agrupado)
        {
            string tipo = grupo.Key;
            bool esCotizacion = tipo.Equals("Cotización", StringComparison.OrdinalIgnoreCase) || tipo.Equals("Cotizacion", StringComparison.OrdinalIgnoreCase);
            bool esRemision = tipo.Contains("Remisión", StringComparison.OrdinalIgnoreCase) || tipo.Contains("Remision", StringComparison.OrdinalIgnoreCase);
            bool esAnticipo = tipo.Equals("Anticipo", StringComparison.OrdinalIgnoreCase);
            bool esNotaCredito = EsNotaCredito(tipo);

            var itemDoc = new ItemTipoDocumentoCorteDto
            {
                TipoComprobante = tipo,
                Cantidad = grupo.Count(),
                MontoTotal = FinancialRoundingHelper.Round2(grupo.Sum(v => v.Total)),
                EsInformativo = esCotizacion || esRemision
            };

            if (esCotizacion || esRemision)
            {
                // Informativo: no aporta ni resta dinero a la gaveta
                itemDoc.MontoEfectivo = 0m;
                itemDoc.MontoElectronico = 0m;
                itemDoc.MontoCreditoCXC = 0m;

                if (esCotizacion)
                {
                    resumen.CantidadCotizaciones += itemDoc.Cantidad;
                    resumen.TotalCotizacionesMonto = FinancialRoundingHelper.Round2(resumen.TotalCotizacionesMonto + itemDoc.MontoTotal);
                }
            }
            else if (esNotaCredito)
            {
                // Nota de Crédito: devolución de mercadería / dinero al cliente
                foreach (var v in grupo)
                {
                    if (v.MetodoPago == "Efectivo")
                    {
                        itemDoc.MontoEfectivo = FinancialRoundingHelper.Round2(itemDoc.MontoEfectivo + v.Total);
                        resumen.TotalDevolucionesEfectivo = FinancialRoundingHelper.Round2(resumen.TotalDevolucionesEfectivo + v.Total);
                    }
                    else
                    {
                        itemDoc.MontoElectronico = FinancialRoundingHelper.Round2(itemDoc.MontoElectronico + v.Total);
                    }
                }
            }
            else if (esAnticipo)
            {
                decimal totalPedidos = 0m;
                decimal anticipoEfectivo = 0m;
                decimal anticipoElectronico = 0m;
                decimal saldoCreditoCXC = 0m;

                foreach (var v in grupo)
                {
                    totalPedidos += v.Total;
                    decimal abonado = Math.Min(v.PagoCon ?? 0m, v.Total);
                    decimal restante = Math.Max(0m, v.Total - abonado);
                    saldoCreditoCXC += restante;

                    if (v.MetodoPago == "Efectivo")
                        anticipoEfectivo += abonado;
                    else
                        anticipoElectronico += abonado;
                }

                itemDoc.MontoEfectivo = FinancialRoundingHelper.Round2(anticipoEfectivo);
                itemDoc.MontoElectronico = FinancialRoundingHelper.Round2(anticipoElectronico);
                itemDoc.MontoCreditoCXC = FinancialRoundingHelper.Round2(saldoCreditoCXC);

                resumen.CantidadAnticipos += itemDoc.Cantidad;
                resumen.AnticiposTotalPedidos = FinancialRoundingHelper.Round2(resumen.AnticiposTotalPedidos + totalPedidos);
                resumen.AnticiposCobradoEfectivo = FinancialRoundingHelper.Round2(resumen.AnticiposCobradoEfectivo + anticipoEfectivo);
                resumen.AnticiposCobradoElectronico = FinancialRoundingHelper.Round2(resumen.AnticiposCobradoElectronico + anticipoElectronico);
                resumen.AnticiposSaldoCXC = FinancialRoundingHelper.Round2(resumen.AnticiposSaldoCXC + saldoCreditoCXC);

                resumen.TotalVentasEfectivo = FinancialRoundingHelper.Round2(resumen.TotalVentasEfectivo + anticipoEfectivo);
                if (anticipoElectronico > 0m)
                {
                    resumen.TotalVentasTarjetas = FinancialRoundingHelper.Round2(resumen.TotalVentasTarjetas + anticipoElectronico);
                }
                resumen.TotalVentasCreditoCXC = FinancialRoundingHelper.Round2(resumen.TotalVentasCreditoCXC + saldoCreditoCXC);
            }
            else
            {
                // Ventas regulares (Ticket, Factura, Crédito Fiscal, Sujeto Excluido, etc.)
                foreach (var v in grupo)
                {
                    if (v.MetodoPago == "Efectivo")
                    {
                        itemDoc.MontoEfectivo = FinancialRoundingHelper.Round2(itemDoc.MontoEfectivo + v.Total);
                        resumen.TotalVentasEfectivo = FinancialRoundingHelper.Round2(resumen.TotalVentasEfectivo + v.Total);
                    }
                    else if (v.MetodoPago == "Tarjeta")
                    {
                        itemDoc.MontoElectronico = FinancialRoundingHelper.Round2(itemDoc.MontoElectronico + v.Total);
                        resumen.TotalVentasTarjetas = FinancialRoundingHelper.Round2(resumen.TotalVentasTarjetas + v.Total);
                    }
                    else if (v.MetodoPago == "Transferencia")
                    {
                        itemDoc.MontoElectronico = FinancialRoundingHelper.Round2(itemDoc.MontoElectronico + v.Total);
                        resumen.TotalVentasTransferencias = FinancialRoundingHelper.Round2(resumen.TotalVentasTransferencias + v.Total);
                    }
                    else if (v.MetodoPago == "Credito")
                    {
                        itemDoc.MontoCreditoCXC = FinancialRoundingHelper.Round2(itemDoc.MontoCreditoCXC + v.Total);
                        resumen.TotalVentasCreditoCXC = FinancialRoundingHelper.Round2(resumen.TotalVentasCreditoCXC + v.Total);
                    }
                    else
                    {
                        itemDoc.MontoEfectivo = FinancialRoundingHelper.Round2(itemDoc.MontoEfectivo + v.Total);
                        resumen.TotalVentasEfectivo = FinancialRoundingHelper.Round2(resumen.TotalVentasEfectivo + v.Total);
                    }
                }
            }

            resumen.Documentos.Add(itemDoc);
        }

        // 2. Abonos a Cuentas por Cobrar recibidos en el período
        resumen.CantidadAbonos = abonos.Count;
        resumen.TotalAbonosEfectivo = FinancialRoundingHelper.Round2(abonos.Where(a => a.MetodoPago == "Efectivo").Sum(a => a.Monto));
        resumen.TotalAbonosElectronico = FinancialRoundingHelper.Round2(abonos.Where(a => a.MetodoPago != "Efectivo").Sum(a => a.Monto));

        // 3. Movimientos de Caja
        resumen.TotalIngresosExtras = FinancialRoundingHelper.Round2(movimientos.Where(m => m.Tipo == "Ingreso").Sum(m => m.Monto));
        resumen.TotalRetiros = FinancialRoundingHelper.Round2(movimientos.Where(m => m.Tipo == "Retiro").Sum(m => m.Monto));
        resumen.TotalGastos = FinancialRoundingHelper.Round2(movimientos.Where(m => m.Tipo == "Gasto").Sum(m => m.Monto));

        // 4. Conteo Físico
        if (desgloseFisico != null)
        {
            resumen.DesgloseFisico = desgloseFisico;
            resumen.EfectivoContado = FinancialRoundingHelper.Round2(desgloseFisico.TotalFisicoContado);
        }
        else if (corte.MontoFinal.HasValue)
        {
            resumen.EfectivoContado = FinancialRoundingHelper.Round2(corte.MontoFinal.Value);
        }

        return resumen;
    }

    private static bool EsNotaCredito(string tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo)) return false;
        string t = tipo.Trim().ToLowerInvariant();
        if (t.Contains("fiscal")) return false; // Credito Fiscal es una venta regular, NO es Nota de Crédito
        return t.Contains("nota de crédito") || t.Contains("nota de credito") || t.StartsWith("n.crédito") || t.StartsWith("n.credito") || t.StartsWith("nc");
    }

    public async Task<(bool Success, string Message, CorteCaja? Corte)> CerrarCajaAsync(int idCorte, decimal montoFinal, decimal? diferencia = null)
    {
        if (montoFinal < 0)
            return (false, "El conteo físico de dinero no puede ser negativo.", null);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var corte = await context.CortesCaja
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.IdCorte == idCorte);

            if (corte == null) return (false, "Turno de caja no encontrado.", null);
            if (corte.Estado == "Cerrada") return (false, "Este turno ya se encuentra cerrado.", null);

            // Generar el balance integral de efectivo para calcular la diferencia matemática exacta
            var resumen = await GenerarResumenCorteAsync(idCorte, "CORTE Z");
            decimal esperado = FinancialRoundingHelper.Round2(resumen.EfectivoEsperado);
            decimal diff = FinancialRoundingHelper.Round2(diferencia ?? (montoFinal - esperado));

            corte.MontoFinal = FinancialRoundingHelper.Round2(montoFinal);
            corte.Diferencia = diff;
            corte.TotalVentas = FinancialRoundingHelper.Round2(resumen.TotalFacturadoNeto);
            corte.FechaCierre = DateTime.Now;
            corte.Estado = "Cerrada";

            await context.SaveChangesAsync();

            string balanceMsg = diff == 0
                ? "Caja cuadrada exactamente."
                : (diff > 0 ? $"Sobrante de +${diff:F2}" : $"Faltante de -${Math.Abs(diff):F2}");

            return (true, $"Caja cerrada con éxito. {balanceMsg}", corte);
        }
        catch (Exception ex)
        {
            return (false, $"Error al cerrar caja: {ex.Message}", null);
        }
    }

    public async Task<List<CorteCaja>> GetHistorialCortesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CortesCaja
            .Include(c => c.Usuario)
            .OrderByDescending(c => c.FechaApertura)
            .Take(60)
            .ToListAsync();
    }
}
