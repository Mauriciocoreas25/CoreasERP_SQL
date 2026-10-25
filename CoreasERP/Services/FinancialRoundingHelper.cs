namespace CoreasERP.Services;

/// <summary>
/// Helper centralizado para redondeo financiero y tributario (Hacienda El Salvador).
/// Aplica MidpointRounding.AwayFromZero (redondeo aritmético estándar)
/// para evitar diferencias de centavos derivadas del redondeo bancario por defecto de .NET.
/// </summary>
public static class FinancialRoundingHelper
{
    /// <summary>
    /// Redondea un importe a 2 decimales (moneda de curso legal USD, subtotales, totales, IVA).
    /// </summary>
    public static decimal Round2(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Redondea a 4 decimales (precios unitarios sin IVA, fracciones, costos unitarios, empaques).
    /// </summary>
    public static decimal Round4(decimal value)
    {
        return Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Redondea a 3 decimales (cantidades de inventario físico, báscula y granel).
    /// </summary>
    public static decimal Round3(decimal value)
    {
        return Math.Round(value, 3, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Desglose de IVA 13% incluido:
    /// SubtotalNeto = Round(Total / 1.13, 2)
    /// IVA = Total - SubtotalNeto (garantiza SubtotalNeto + IVA == Total exactamente)
    /// </summary>
    public static (decimal SubtotalNeto, decimal Iva) DesglosarIvaIncluido(decimal totalConIva)
    {
        decimal total = Round2(totalConIva);
        decimal subtotalNeto = Round2(total / 1.13m);
        decimal iva = Round2(total - subtotalNeto);
        return (subtotalNeto, iva);
    }

    /// <summary>
    /// Cálculo de Crédito Fiscal (precios sin IVA):
    /// Total = SubtotalGravado + Round(SubtotalGravado * 0.13, 2)
    /// </summary>
    public static (decimal SubtotalGravado, decimal Iva, decimal Total) CalcularCreditoFiscal(decimal subtotalGravado)
    {
        decimal subtotal = Round2(subtotalGravado);
        decimal iva = Round2(subtotal * 0.13m);
        decimal total = Round2(subtotal + iva);
        return (subtotal, iva, total);
    }
}
