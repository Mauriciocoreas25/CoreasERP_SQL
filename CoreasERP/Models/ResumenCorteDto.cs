using CoreasERP.Services;

namespace CoreasERP.Models;

public class DesgloseDenominacionDto
{
    // Billetes USD
    public int Billetes100 { get; set; }
    public int Billetes50 { get; set; }
    public int Billetes20 { get; set; }
    public int Billetes10 { get; set; }
    public int Billetes5 { get; set; }
    public int Billetes1 { get; set; }

    // Monedas USD
    public int Monedas1 { get; set; }       // $1.00 moneda
    public int Monedas025 { get; set; }     // $0.25 (Cora / Quarter)
    public int Monedas010 { get; set; }     // $0.10 (Dime)
    public int Monedas005 { get; set; }     // $0.05 (Nickel)
    public int Monedas001 { get; set; }     // $0.01 (Penny)

    public decimal TotalBilletes =>
        (Billetes100 * 100m) +
        (Billetes50 * 50m) +
        (Billetes20 * 20m) +
        (Billetes10 * 10m) +
        (Billetes5 * 5m) +
        (Billetes1 * 1m);

    public decimal TotalMonedas =>
        (Monedas1 * 1.00m) +
        (Monedas025 * 0.25m) +
        (Monedas010 * 0.10m) +
        (Monedas005 * 0.05m) +
        (Monedas001 * 0.01m);

    public decimal TotalFisicoContado => TotalBilletes + TotalMonedas;
}

public class ItemTipoDocumentoCorteDto
{
    public string TipoComprobante { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoEfectivo { get; set; }
    public decimal MontoElectronico { get; set; }
    public decimal MontoCreditoCXC { get; set; }
    public bool EsInformativo { get; set; } // true para Cotizaciones (no afecta caja ni ventas)
}

public class ResumenCorteDto
{
    public int IdCorte { get; set; }
    public string TipoCorte { get; set; } = "CORTE X"; // 'CORTE X' (Parcial) o 'CORTE Z' (Definitivo)
    public string NombreCajero { get; set; } = "Cajero";
    public string NombreEmpresa { get; set; } = "Coreas ERP";
    public string Sucursal { get; set; } = "M001";
    public string PuntoVenta { get; set; } = "P001";
    public DateTime FechaApertura { get; set; }
    public DateTime FechaCorte { get; set; } = DateTime.Now;
    public string EstadoTurno { get; set; } = "Abierta";

    // Fondo Inicial
    public decimal FondoInicial { get; set; }

    // Desglose por Tipo de Comprobante
    public List<ItemTipoDocumentoCorteDto> Documentos { get; set; } = new();

    public int TotalDocumentosEmitidos => Documentos.Where(d => !d.EsInformativo).Sum(d => d.Cantidad);
    public decimal TotalFacturadoNeto => FinancialRoundingHelper.Round2(Documentos.Where(d => !d.EsInformativo).Sum(d => d.MontoTotal));

    // Resumen de Cotizaciones (Informativo, $0 en caja)
    public int CantidadCotizaciones { get; set; }
    public decimal TotalCotizacionesMonto { get; set; }

    // Resumen de Anticipos (Pago mixto: abono a caja + saldo a CXC)
    public int CantidadAnticipos { get; set; }
    public decimal AnticiposTotalPedidos { get; set; }
    public decimal AnticiposCobradoEfectivo { get; set; }
    public decimal AnticiposCobradoElectronico { get; set; }
    public decimal AnticiposSaldoCXC { get; set; }

    // Resumen por Formas de Pago
    public decimal TotalVentasEfectivo { get; set; }
    public decimal TotalVentasTarjetas { get; set; }
    public decimal TotalVentasTransferencias { get; set; }
    public decimal TotalVentasCreditoCXC { get; set; }

    // Abonos recibidos de Cuentas por Cobrar en el turno
    public int CantidadAbonos { get; set; }
    public decimal TotalAbonosEfectivo { get; set; }
    public decimal TotalAbonosElectronico { get; set; }

    // Movimientos de Caja (Retiros, Gastos, Ingresos manuales)
    public List<MovimientoCaja> Movimientos { get; set; } = new();
    public decimal TotalIngresosExtras { get; set; }
    public decimal TotalRetiros { get; set; }
    public decimal TotalGastos { get; set; }
    public decimal TotalDevolucionesEfectivo { get; set; }

    // Totales calculados de Flujo de Efectivo
    public decimal TotalEntradasEfectivo => FinancialRoundingHelper.Round2(TotalVentasEfectivo + TotalAbonosEfectivo + TotalIngresosExtras);
    public decimal TotalSalidasEfectivo => FinancialRoundingHelper.Round2(TotalRetiros + TotalGastos + TotalDevolucionesEfectivo);
    public decimal EfectivoEsperado => FinancialRoundingHelper.Round2(FondoInicial + TotalEntradasEfectivo - TotalSalidasEfectivo);

    // Arqueo físico y Cuadre
    public decimal? EfectivoContado { get; set; }
    public decimal? Diferencia => EfectivoContado.HasValue ? FinancialRoundingHelper.Round2(EfectivoContado.Value - EfectivoEsperado) : null;

    public string EstadoCuadre
    {
        get
        {
            if (!EfectivoContado.HasValue || !Diferencia.HasValue) return "Pendiente de Conteo";
            if (Diferencia.Value == 0m) return "Caja Cuadrada Exacta";
            if (Diferencia.Value > 0m) return $"Sobrante (+${Diferencia.Value:F2})";
            return $"Faltante (-${Math.Abs(Diferencia.Value):F2})";
        }
    }

    // Desglose de denominaciones físicas contadas
    public DesgloseDenominacionDto? DesgloseFisico { get; set; }
}
