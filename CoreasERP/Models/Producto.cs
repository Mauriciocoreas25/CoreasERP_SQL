using System.ComponentModel.DataAnnotations.Schema;
using CoreasERP.Services;

namespace CoreasERP.Models;

public class Producto
{
    public int IdProducto { get; set; }
    public int IdCategoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal PrecioCompra { get; set; } = 0.00m;
    public decimal PrecioVenta { get; set; } = 0.00m;
    public decimal? PrecioGranel { get; set; }
    public decimal? CostoGranel { get; set; }
    public decimal? CantidadPorEmpaque { get; set; } = 1.00m;
    public decimal Stock { get; set; } = 0.000m;
    public decimal StockMinimo { get; set; } = 5.000m;
    public bool EsGranel { get; set; } = false;
    public string? UnidadMedida { get; set; } = "Unidad";
    public string? Imagen { get; set; }
    public string? ImagenUrl { get; set; }
    public decimal MargenExcepcion { get; set; } = 0.00m;
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public Categoria? Categoria { get; set; }
    public ICollection<DetalleVenta> DetalleVentas { get; set; } = new List<DetalleVenta>();
    public ICollection<ProductoMaterial> Materiales { get; set; } = new List<ProductoMaterial>();
    public ICollection<AjusteInventario> AjustesInventario { get; set; } = new List<AjusteInventario>();

    [NotMapped]
    public decimal MargenGanancia => PrecioCompra > 0 ? FinancialRoundingHelper.Round2(((PrecioVenta - PrecioCompra) / PrecioCompra) * 100m) : 0m;

    [NotMapped]
    public decimal CostoEfectivoGranel => (CostoGranel.HasValue && CostoGranel.Value > 0)
        ? CostoGranel.Value
        : (CantidadPorEmpaque.HasValue && CantidadPorEmpaque.Value > 0 ? FinancialRoundingHelper.Round4(PrecioCompra / CantidadPorEmpaque.Value) : PrecioCompra);

    [NotMapped]
    public decimal MargenGananciaGranel
    {
        get
        {
            decimal c = CostoEfectivoGranel;
            if (c <= 0 || !PrecioGranel.HasValue || PrecioGranel.Value <= 0) return 0m;
            return FinancialRoundingHelper.Round2(((PrecioGranel.Value - c) / c) * 100m);
        }
    }
}
