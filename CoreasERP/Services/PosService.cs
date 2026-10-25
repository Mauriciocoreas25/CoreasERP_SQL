using Microsoft.EntityFrameworkCore;
using CoreasERP.Data;
using CoreasERP.Models;
using CoreasERP.Models.Dte;
using CoreasERP.Services.Dte;

namespace CoreasERP.Services;

public class CartItemDto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public decimal Cantidad { get; set; } = 1.000m;
    public decimal Descuento { get; set; } = 0.00m;
    public decimal StockDisponible { get; set; }
    public bool EsGranel { get; set; } = false;
    public decimal CantidadPorEmpaque { get; set; } = 1.00m;
    public string UnidadMedida { get; set; } = "Unidad";
    public decimal CostoUnitario { get; set; } = 0.00m;
    public decimal Subtotal => FinancialRoundingHelper.Round2((Cantidad * Precio) - Descuento);
    public decimal CostoSubtotal => FinancialRoundingHelper.Round2(Cantidad * CostoUnitario);
    public decimal CantidadBaseStock => (EsGranel && CantidadPorEmpaque > 1) ? FinancialRoundingHelper.Round3(Cantidad / CantidadPorEmpaque) : FinancialRoundingHelper.Round3(Cantidad);
}

public interface IPosService
{
    Task<(bool Success, string Message, Venta? Venta)> RegistrarVentaAsync(
        int idCliente,
        int idUsuario,
        string tipoComprobante,
        string metodoPago,
        decimal pagoCon,
        List<CartItemDto> items);

    Task<(bool Success, string Message)> AnularVentaAsync(int idVenta);
    Task<List<Venta>> GetVentasAsync(DateTime? fecha = null, string? busqueda = null);
    Task<Venta?> GetVentaByIdAsync(int idVenta);

    Task<(bool Success, string Message, decimal TotalCostoMerma, List<Merma> Mermas)> RegistrarMermaLoteAsync(
        List<CartItemDto> items,
        string motivo,
        int idSupervisor,
        int idCajero,
        string? notaAdicional = null);
}

public class PosService : IPosService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IDteJsonBuilderService _dteJsonBuilder;
    private readonly IFelSvApiService _felSvApiService;

    public PosService(
        IDbContextFactory<AppDbContext> contextFactory,
        IDteJsonBuilderService dteJsonBuilder,
        IFelSvApiService felSvApiService)
    {
        _contextFactory = contextFactory;
        _dteJsonBuilder = dteJsonBuilder;
        _felSvApiService = felSvApiService;
    }

    public async Task<(bool Success, string Message, Venta? Venta)> RegistrarVentaAsync(
        int idCliente,
        int idUsuario,
        string tipoComprobante,
        string metodoPago,
        decimal pagoCon,
        List<CartItemDto> items)
    {
        if (items == null || !items.Any())
            return (false, "El carrito de compras está vacío.", null);

        decimal total = FinancialRoundingHelper.Round2(items.Sum(i => i.Subtotal));
        if (total <= 0)
            return (false, "El total de la venta debe ser mayor a cero.", null);

        bool esAnticipo = tipoComprobante.Equals("Anticipo", StringComparison.OrdinalIgnoreCase);
        bool esCotizacion = tipoComprobante.Equals("Cotización", StringComparison.OrdinalIgnoreCase);

        pagoCon = FinancialRoundingHelper.Round2(pagoCon);

        if (!esAnticipo && !esCotizacion && metodoPago == "Efectivo" && pagoCon < total)
        {
            return (false, $"Monto recibido (${pagoCon:F2}) es menor al total a pagar (${total:F2}).", null);
        }

        if (esAnticipo && pagoCon < 0)
        {
            return (false, "El monto del anticipo no puede ser negativo.", null);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // 1. Validar existencias físicas en la base de datos bajo transacción (solo para ventas reales, no presupuestos)
            if (!esCotizacion)
            {
                foreach (var item in items)
                {
                    var producto = await context.Productos.FindAsync(item.IdProducto);
                    if (producto == null || !producto.Estado)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"El producto '{item.Nombre}' ya no está disponible en el catálogo.", null);
                    }

                    if (producto.Stock <= 0)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"El producto '{producto.Nombre}' no tiene existencias disponibles (Stock: 0).", null);
                    }

                    decimal factor = (item.EsGranel && item.CantidadPorEmpaque > 1) ? item.CantidadPorEmpaque : 1m;
                    decimal cantADescontar = item.Cantidad / factor;

                    if (producto.Stock < cantADescontar)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock:G29}, solicitado equivalente: {cantADescontar:G29}.", null);
                    }

                    // Descontar inventario físico
                    producto.Stock -= cantADescontar;
                }
            }

            // 2. Cálculos financieros (IVA 13% incluido en precio, redondeo exacto Hacienda)
            var (subtotal, impuesto) = FinancialRoundingHelper.DesglosarIvaIncluido(total);
            decimal montoAbonado = esAnticipo ? FinancialRoundingHelper.Round2(Math.Min(pagoCon, total)) : (esCotizacion ? 0m : total);
            decimal saldoPendiente = esAnticipo ? FinancialRoundingHelper.Round2(Math.Max(0m, total - pagoCon)) : 0m;
            decimal? cambio = (!esCotizacion && metodoPago == "Efectivo" && pagoCon > total) ? FinancialRoundingHelper.Round2(pagoCon - total) : 0.00m;

            // 3. Generar correlativo DTE / Comprobante
            var config = await context.ConfiguracionNegocio.FirstOrDefaultAsync();
            bool dteHabilitado = config?.HabilitarDte ?? true;
            string? codigoDte = dteHabilitado ? _dteJsonBuilder.MapearTipoComprobanteADte(tipoComprobante) : null;

            int correlativoDte = 1;
            if (codigoDte != null)
            {
                // Buscar el correlativo más alto existente para este tipo de DTE para evitar duplicados en MH
                string patronControl = $"DTE-{codigoDte}-";
                var ultimosControles = await context.Ventas
                    .Where(v => v.NumeroControl != null && v.NumeroControl.StartsWith(patronControl))
                    .Select(v => v.NumeroControl!)
                    .ToListAsync();

                int maxCorrelativo = 0;
                foreach (var ctrl in ultimosControles)
                {
                    // Formato: DTE-XX-MXXXPXXX-000000000000XXX (últimos 15 dígitos)
                    var partes = ctrl.Split('-');
                    if (partes.Length >= 4 && int.TryParse(partes[3], out int num))
                    {
                        if (num > maxCorrelativo) maxCorrelativo = num;
                    }
                }

                // Iniciar en al menos 101 para evitar colisiones con correlativos de pruebas históricas del emisor
                correlativoDte = Math.Max(101, maxCorrelativo + 1);
            }

            string codEstable = config?.CodEstablecimiento ?? "M001";
            string codPunto = config?.CodPuntoVenta ?? "P001";

            string? numControl = codigoDte != null
                ? _dteJsonBuilder.GenerarNumeroControl(codigoDte, codEstable, codPunto, correlativoDte)
                : null;
            string? codGeneracion = codigoDte != null
                ? _dteJsonBuilder.GenerarCodigoGeneracion()
                : null;

            string prefijo = tipoComprobante switch
            {
                "Factura" => "F",
                "Credito Fiscal" => "CF",
                "Anticipo" => "ANT",
                _ => "T"
            };
            string fechaStr = DateTime.Now.ToString("yyyyMMdd");
            int ventasHoy = await context.Ventas
                .CountAsync(v => v.FechaHora.Date == DateTime.Today);
            string numComprobante = numControl ?? $"{prefijo}-{fechaStr}-{(ventasHoy + 1):D4}";

            // 4. Crear registro maestro de Venta
            var venta = new Venta
            {
                IdCliente = idCliente,
                IdUsuario = idUsuario,
                TipoComprobante = tipoComprobante,
                NumComprobante = numComprobante,
                NumeroControl = numControl,
                CodigoGeneracion = codGeneracion,
                EstadoDte = codigoDte != null ? "Pendiente" : "No_Aplica",
                MetodoPago = metodoPago,
                Subtotal = subtotal,
                Impuesto = impuesto,
                Total = total,
                PagoCon = esCotizacion ? 0m : (metodoPago == "Efectivo" ? pagoCon : (esAnticipo ? pagoCon : total)),
                Cambio = cambio,
                Estado = esCotizacion ? "Cotización" : "Completada",
                FechaHora = DateTime.Now
            };

            await context.Ventas.AddAsync(venta);
            await context.SaveChangesAsync();

            // 5. Crear detalles de venta
            foreach (var item in items)
            {
                var detalle = new DetalleVenta
                {
                    IdVenta = venta.IdVenta,
                    IdProducto = item.IdProducto,
                    Cantidad = FinancialRoundingHelper.Round3(item.Cantidad),
                    Precio = FinancialRoundingHelper.Round4(item.Precio),
                    Descuento = FinancialRoundingHelper.Round2(item.Descuento),
                    Subtotal = FinancialRoundingHelper.Round2(item.Subtotal)
                };
                await context.DetalleVentas.AddAsync(detalle);
            }

            // 6. Si es Anticipo, generar Cuenta por Cobrar y registrar Abono inicial si hubo pago
            if (esAnticipo)
            {
                var cuentaCobrar = new CuentaCobrar
                {
                    IdVenta = venta.IdVenta,
                    IdCliente = idCliente,
                    MontoTotal = total,
                    SaldoPendiente = saldoPendiente,
                    FechaEmision = DateTime.Now,
                    FechaVencimiento = DateTime.Now.AddDays(30),
                    Estado = saldoPendiente > 0 ? "Pendiente" : "Pagada"
                };
                await context.CuentasCobrar.AddAsync(cuentaCobrar);
                await context.SaveChangesAsync();

                if (montoAbonado > 0)
                {
                    var abonoInicial = new Abono
                    {
                        IdCuentaCobrar = cuentaCobrar.IdCuentaCobrar,
                        IdUsuario = idUsuario,
                        Monto = montoAbonado,
                        MetodoPago = metodoPago,
                        FechaHora = DateTime.Now,
                        Notas = $"Anticipo inicial en comprobante {venta.NumComprobante}"
                    };
                    await context.Abonos.AddAsync(abonoInicial);
                    await context.SaveChangesAsync();
                }
            }

            // 7. Si el pago es en efectivo, acumular al turno de caja activo solo el dinero real ingresado (las cotizaciones no suman dinero)
            if (metodoPago == "Efectivo" && !esCotizacion)
            {
                var corteActivo = await context.CortesCaja
                    .Where(c => c.IdUsuario == idUsuario && c.Estado == "Abierta")
                    .OrderByDescending(c => c.FechaApertura)
                    .FirstOrDefaultAsync();

                if (corteActivo != null)
                {
                    // Para anticipos en efectivo ingresa únicamente lo pagado físicamente, no el crédito total
                    decimal ingresoEfectivo = esAnticipo ? montoAbonado : total;
                    corteActivo.TotalVentas = FinancialRoundingHelper.Round2(corteActivo.TotalVentas + ingresoEfectivo);
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Cargar datos de navegación para retornar comprobante completo
            await context.Entry(venta).Reference(v => v.Cliente).LoadAsync();
            await context.Entry(venta).Reference(v => v.Usuario).LoadAsync();
            await context.Entry(venta).Collection(v => v.DetalleVentas).Query().Include(d => d.Producto).LoadAsync();

            // 8. Transmisión DTE oficial a la API FelSv (Ministerio de Hacienda)
            if (codigoDte != null && (config?.HabilitarDte ?? true) && venta.Cliente != null)
            {
                try
                {
                    var dtePayload = _dteJsonBuilder.ConstruirPayloadDte(
                        codigoDte,
                        venta,
                        venta.Cliente,
                        config ?? new ConfiguracionNegocio(),
                        correlativoDte);

                    if (!string.IsNullOrEmpty(numControl)) dtePayload.NumeroControl = numControl;

                    var dteResult = await _felSvApiService.EmitirDteAsync(dtePayload, config?.FelSvTenantId);

                    await using var updateCtx = await _contextFactory.CreateDbContextAsync();
                    var vDb = await updateCtx.Ventas.FindAsync(venta.IdVenta);
                    if (vDb != null)
                    {
                        vDb.DteId = dteResult.DteId;
                        vDb.SelloRecepcion = dteResult.SelloRecepcion;
                        vDb.EstadoDte = dteResult.Estado ?? (dteResult.Success ? "Transmitido" : "Contingencia_Pendiente");
                        vDb.RespuestaHacienda = dteResult.MensajeHacienda;
                        await updateCtx.SaveChangesAsync();

                        venta.DteId = vDb.DteId;
                        venta.SelloRecepcion = vDb.SelloRecepcion;
                        venta.EstadoDte = vDb.EstadoDte;
                        venta.RespuestaHacienda = vDb.RespuestaHacienda;
                    }
                }
                catch (Exception dteEx)
                {
                    // Ante cualquier desconexión con FelSv, guardar en contingencia diferida local
                    await using var updateCtx = await _contextFactory.CreateDbContextAsync();
                    var vDb = await updateCtx.Ventas.FindAsync(venta.IdVenta);
                    if (vDb != null)
                    {
                        vDb.EstadoDte = "Contingencia_Pendiente";
                        vDb.RespuestaHacienda = $"Contingencia local: {dteEx.Message}";
                        await updateCtx.SaveChangesAsync();

                        venta.EstadoDte = vDb.EstadoDte;
                        venta.RespuestaHacienda = vDb.RespuestaHacienda;
                    }
                }
            }

            return (true, "Venta registrada exitosamente.", venta);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Error al procesar la venta: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> AnularVentaAsync(int idVenta)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var venta = await context.Ventas
                .Include(v => v.DetalleVentas)
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .FirstOrDefaultAsync(v => v.IdVenta == idVenta);

            if (venta == null)
            {
                await transaction.RollbackAsync();
                return (false, "La venta especificada no existe.");
            }

            if (venta.Estado == "Anulada")
            {
                await transaction.RollbackAsync();
                return (false, "Esta venta ya fue anulada anteriormente.");
            }

            // Reintegrar las existencias al inventario físico
            foreach (var detalle in venta.DetalleVentas)
            {
                var producto = await context.Productos.FindAsync(detalle.IdProducto);
                if (producto != null)
                {
                    decimal factor = (producto.CantidadPorEmpaque.HasValue && producto.CantidadPorEmpaque.Value > 1 && producto.PrecioGranel.HasValue && detalle.Precio == producto.PrecioGranel.Value)
                        ? producto.CantidadPorEmpaque.Value
                        : 1m;
                    producto.Stock = FinancialRoundingHelper.Round3(producto.Stock + (detalle.Cantidad / factor));
                }
            }

            // Cambiar estado a Anulada
            venta.Estado = "Anulada";

            // Si el DTE estaba transmitido a Hacienda, enviar solicitud de invalidación (CAT-024)
            if (venta.EstadoDte == "Transmitido" && !string.IsNullOrEmpty(venta.DteId))
            {
                try
                {
                    var config = await context.ConfiguracionNegocio.FirstOrDefaultAsync();
                    var invalReq = new InvalidateDteRequestDto
                    {
                        DteId = venta.DteId,
                        TipoAnulacion = 2, // Solicitud del receptor
                        MotivoAnulacion = "Anulación por devolución de compra o solicitud del cliente",
                        NombreResponsable = venta.Usuario?.Nombre ?? "Cajero de Turno",
                        TipDocResponsable = "36",
                        NumDocResponsable = "06140101001011",
                        NombreSolicita = venta.Cliente?.Nombre ?? "Cliente General",
                        TipDocSolicita = "13",
                        NumDocSolicita = !string.IsNullOrWhiteSpace(venta.Cliente?.NumDocumento) ? venta.Cliente.NumDocumento.Replace("-", "").Trim() : "000000000"
                    };
                    var invalRes = await _felSvApiService.InvalidarDteAsync(invalReq, config?.FelSvTenantId);
                    if (invalRes.Success)
                    {
                        venta.EstadoDte = "Invalidado";
                        venta.RespuestaHacienda = invalRes.Mensaje ?? "DTE Invalidado en MH";
                    }
                }
                catch { }
            }

            // Si la venta era en efectivo y hay un corte de caja abierto del mismo usuario, ajustar
            if (venta.MetodoPago == "Efectivo")
            {
                var corteActivo = await context.CortesCaja
                    .Where(c => c.IdUsuario == venta.IdUsuario && c.Estado == "Abierta")
                    .OrderByDescending(c => c.FechaApertura)
                    .FirstOrDefaultAsync();

                if (corteActivo != null && corteActivo.TotalVentas >= venta.Total)
                {
                    corteActivo.TotalVentas = FinancialRoundingHelper.Round2(corteActivo.TotalVentas - venta.Total);
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, $"Venta {venta.NumComprobante} anulada y stock reintegrado correctamente.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Error al anular venta: {ex.Message}");
        }
    }

    public async Task<List<Venta>> GetVentasAsync(DateTime? fecha = null, string? busqueda = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Usuario)
            .Include(v => v.DetalleVentas)
                .ThenInclude(d => d.Producto)
            .AsQueryable();

        if (fecha.HasValue)
        {
            var date = fecha.Value.Date;
            query = query.Where(v => v.FechaHora.Date == date);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var search = busqueda.Trim().ToLower();
            query = query.Where(v =>
                v.NumComprobante.ToLower().Contains(search) ||
                (v.Cliente != null && v.Cliente.Nombre.ToLower().Contains(search)));
        }

        return await query.OrderByDescending(v => v.FechaHora).ToListAsync();
    }

    public async Task<Venta?> GetVentaByIdAsync(int idVenta)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Usuario)
            .Include(v => v.DetalleVentas)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(v => v.IdVenta == idVenta);
    }

    public async Task<(bool Success, string Message, decimal TotalCostoMerma, List<Merma> Mermas)> RegistrarMermaLoteAsync(
        List<CartItemDto> items,
        string motivo,
        int idSupervisor,
        int idCajero,
        string? notaAdicional = null)
    {
        if (items == null || !items.Any())
            return (false, "El carrito no contiene productos para dar de baja.", 0m, new List<Merma>());

        if (string.IsNullOrWhiteSpace(motivo))
            return (false, "Debe especificar el motivo de la merma o salida interna.", 0m, new List<Merma>());

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var supervisor = await context.Usuarios.FindAsync(idSupervisor);
            var cajero = await context.Usuarios.FindAsync(idCajero);
            string nombreSupervisor = supervisor?.Nombre ?? "Supervisor";
            string nombreCajero = cajero?.Nombre ?? "Cajero";

            decimal totalCostoMerma = 0m;
            var listaMermas = new List<Merma>();

            foreach (var item in items)
            {
                var prod = await context.Productos.FindAsync(item.IdProducto);
                if (prod == null) continue;

                // 1. Costo unitario según la presentación seleccionada:
                // Si se descargó como Granel (ej. 1 bola de jabón), se toma el costo por bola (prod.CostoEfectivoGranel).
                // Si se descargó como Empaque completo (ej. 1 paquete de 3 bolas), se toma el costo del empaque (prod.PrecioCompra).
                decimal costoUnitario = item.EsGranel
                    ? prod.CostoEfectivoGranel
                    : (prod.PrecioCompra > 0 ? prod.PrecioCompra : 0m);

                // 2. Pérdida contable real a costo:
                // Cantidad física dada de baja (item.Cantidad) x costo unitario de esa presentación (costoUnitario)
                decimal costoLinea = FinancialRoundingHelper.Round2(item.Cantidad * costoUnitario);
                totalCostoMerma = FinancialRoundingHelper.Round2(totalCostoMerma + costoLinea);

                // 3. Deducción del inventario físico base (prod.Stock se almacena en empaques base):
                decimal cantBaseDeducir = item.CantidadBaseStock;
                prod.Stock = FinancialRoundingHelper.Round3(Math.Max(0m, prod.Stock - cantBaseDeducir));

                var merma = new Merma
                {
                    IdProducto = prod.IdProducto,
                    IdUsuarioSupervisor = idSupervisor,
                    IdUsuarioCajero = idCajero,
                    Cantidad = item.Cantidad,
                    CantidadBaseStock = cantBaseDeducir,
                    CostoUnitario = costoUnitario,
                    CostoTotal = costoLinea,
                    Motivo = motivo.Trim(),
                    Observaciones = !string.IsNullOrWhiteSpace(notaAdicional) ? notaAdicional.Trim() : null,
                    FechaHora = DateTime.Now,
                    Producto = prod,
                    UsuarioSupervisor = supervisor,
                    UsuarioCajero = cajero
                };

                await context.Mermas.AddAsync(merma);
                listaMermas.Add(merma);
            }

            await context.SaveChangesAsync();

            return (true, $"Se dio de baja {items.Count} producto(s) por merma/consumo interno con un costo total de ${totalCostoMerma:N2}. Autorizado por: {nombreSupervisor}.", totalCostoMerma, listaMermas);
        }
        catch (Exception ex)
        {
            return (false, $"Error al registrar la merma en lote: {ex.Message}", 0m, new List<Merma>());
        }
    }
}
