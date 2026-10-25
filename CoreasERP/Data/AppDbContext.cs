using Microsoft.EntityFrameworkCore;
using CoreasERP.Models;

namespace CoreasERP.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<CorteCaja> CortesCaja => Set<CorteCaja>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetalleVentas => Set<DetalleVenta>();
    public DbSet<ClienteCuenta> ClienteCuentas => Set<ClienteCuenta>();
    public DbSet<OrdenWeb> OrdenesWeb => Set<OrdenWeb>();
    public DbSet<DetalleOrdenWeb> DetalleOrdenesWeb => Set<DetalleOrdenWeb>();

    // Módulos extendidos
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<DetalleCompra> DetalleCompras => Set<DetalleCompra>();
    public DbSet<Promocion> Promociones => Set<Promocion>();
    public DbSet<CuentaCobrar> CuentasCobrar => Set<CuentaCobrar>();
    public DbSet<Abono> Abonos => Set<Abono>();
    public DbSet<ConfiguracionNegocio> ConfiguracionNegocio => Set<ConfiguracionNegocio>();
    public DbSet<ProductoMaterial> ProductoMateriales => Set<ProductoMaterial>();
    public DbSet<AjusteInventario> AjustesInventario => Set<AjusteInventario>();
    public DbSet<Merma> Mermas => Set<Merma>();
    public DbSet<CambioPrecio> CambiosPrecio => Set<CambioPrecio>();
    public DbSet<MovimientoCaja> MovimientosCaja => Set<MovimientoCaja>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Roles
        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.IdRol);
            entity.Property(e => e.Nombre).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(150);
        });

        // Usuarios
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(e => e.IdUsuario);
            entity.Property(e => e.NombreUsuario).HasColumnName("Usuario").HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.NombreUsuario).IsUnique();
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Telefono).HasMaxLength(30);
            entity.Property(e => e.Clave).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(e => e.IdRol)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Categorias
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("Categorias");
            entity.HasKey(e => e.IdCategoria);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.Estado).HasDefaultValue(true);
        });

        // Productos
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("Productos");
            entity.HasKey(e => e.IdProducto);
            entity.Property(e => e.Codigo).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.PrecioCompra).HasPrecision(18, 4);
            entity.Property(e => e.PrecioVenta).HasPrecision(18, 4);
            entity.Property(e => e.PrecioGranel).HasPrecision(18, 4);
            entity.Property(e => e.CostoGranel).HasPrecision(18, 4);
            entity.Property(e => e.CantidadPorEmpaque).HasPrecision(10, 2).HasDefaultValue(1.00m);
            entity.Property(e => e.Stock).HasPrecision(12, 3).HasDefaultValue(0.000m);
            entity.Property(e => e.StockMinimo).HasPrecision(12, 3).HasDefaultValue(5.000m);
            entity.Property(e => e.EsGranel).HasDefaultValue(false);
            entity.Property(e => e.UnidadMedida).HasMaxLength(30).HasDefaultValue("Unidad");
            entity.Property(e => e.Imagen).HasMaxLength(255);
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(e => e.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Clientes
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(e => e.IdCliente);
            entity.Property(e => e.TipoPersona).HasMaxLength(30).HasDefaultValue("Natural");
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.TipoDocumento).HasMaxLength(30).HasDefaultValue("DUI");
            entity.Property(e => e.NumDocumento).HasMaxLength(50);
            entity.Property(e => e.Telefono).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Direccion).HasMaxLength(255);
            entity.Property(e => e.Nrc).HasMaxLength(50);
            entity.Property(e => e.Giro).HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // CortesCaja
        modelBuilder.Entity<CorteCaja>(entity =>
        {
            entity.ToTable("CortesCaja");
            entity.HasKey(e => e.IdCorte);
            entity.Property(e => e.MontoInicial).HasPrecision(18, 2);
            entity.Property(e => e.TotalVentas).HasPrecision(18, 2);
            entity.Property(e => e.MontoFinal).HasPrecision(18, 2);
            entity.Property(e => e.Diferencia).HasPrecision(18, 2);
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Abierta");
            entity.Property(e => e.FechaApertura).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.CortesCaja)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Ventas
        modelBuilder.Entity<Venta>(entity =>
        {
            entity.ToTable("Ventas");
            entity.HasKey(e => e.IdVenta);
            entity.Property(e => e.TipoComprobante).HasMaxLength(30).HasDefaultValue("Ticket");
            entity.Property(e => e.NumComprobante).HasMaxLength(50).IsRequired();
            entity.Property(e => e.MetodoPago).HasMaxLength(50).HasDefaultValue("Efectivo");
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            entity.Property(e => e.Impuesto).HasPrecision(18, 2);
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.PagoCon).HasPrecision(18, 2);
            entity.Property(e => e.Cambio).HasPrecision(18, 2);
            entity.Property(e => e.Estado).HasMaxLength(30).HasDefaultValue("Completada");
            entity.Property(e => e.FechaHora).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CodigoGeneracion).HasMaxLength(36);
            entity.Property(e => e.NumeroControl).HasMaxLength(35);
            entity.Property(e => e.SelloRecepcion).HasMaxLength(100);
            entity.Property(e => e.EstadoDte).HasMaxLength(30).HasDefaultValue("No_Aplica");
            entity.Property(e => e.DteId).HasMaxLength(50);
            entity.Property(e => e.RespuestaHacienda).HasMaxLength(500);

            entity.HasOne(e => e.Cliente)
                .WithMany(c => c.Ventas)
                .HasForeignKey(e => e.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.Ventas)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DetalleVentas
        modelBuilder.Entity<DetalleVenta>(entity =>
        {
            entity.ToTable("DetalleVentas");
            entity.HasKey(e => e.IdDetalle);
            entity.Property(e => e.Cantidad).HasPrecision(12, 3);
            entity.Property(e => e.Precio).HasPrecision(18, 4);
            entity.Property(e => e.Descuento).HasPrecision(18, 2).HasDefaultValue(0.00m);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);

            entity.HasOne(e => e.Venta)
                .WithMany(v => v.DetalleVentas)
                .HasForeignKey(e => e.IdVenta)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Producto)
                .WithMany(p => p.DetalleVentas)
                .HasForeignKey(e => e.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Proveedores
        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.ToTable("Proveedores");
            entity.HasKey(e => e.IdProveedor);
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Contacto).HasMaxLength(100);
            entity.Property(e => e.Telefono).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Direccion).HasMaxLength(255);
            entity.Property(e => e.Nrc).HasMaxLength(50);
            entity.Property(e => e.Nit).HasMaxLength(50);
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Compras
        modelBuilder.Entity<Compra>(entity =>
        {
            entity.ToTable("Compras");
            entity.HasKey(e => e.IdCompra);
            entity.Property(e => e.NumFactura).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.Estado).HasMaxLength(30).HasDefaultValue("Completada");
            entity.Property(e => e.FechaHora).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Proveedor)
                .WithMany(p => p.Compras)
                .HasForeignKey(e => e.IdProveedor)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DetalleCompras
        modelBuilder.Entity<DetalleCompra>(entity =>
        {
            entity.ToTable("DetalleCompras");
            entity.HasKey(e => e.IdDetalleCompra);
            entity.Property(e => e.Cantidad).HasPrecision(12, 3);
            entity.Property(e => e.PrecioCompra).HasPrecision(18, 4);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);

            entity.HasOne(e => e.Compra)
                .WithMany(c => c.DetalleCompras)
                .HasForeignKey(e => e.IdCompra)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Promociones
        modelBuilder.Entity<Promocion>(entity =>
        {
            entity.ToTable("Promociones");
            entity.HasKey(e => e.IdPromocion);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.TipoDescuento).HasMaxLength(30).HasDefaultValue("Porcentaje");
            entity.Property(e => e.Valor).HasPrecision(10, 2);
            entity.Property(e => e.Estado).HasDefaultValue(true);

            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.IdProducto)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // CuentasCobrar
        modelBuilder.Entity<CuentaCobrar>(entity =>
        {
            entity.ToTable("CuentasCobrar");
            entity.HasKey(e => e.IdCuentaCobrar);
            entity.Property(e => e.MontoTotal).HasPrecision(18, 2);
            entity.Property(e => e.SaldoPendiente).HasPrecision(18, 2);
            entity.Property(e => e.Estado).HasMaxLength(30).HasDefaultValue("Pendiente");
            entity.Property(e => e.FechaEmision).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Venta)
                .WithMany()
                .HasForeignKey(e => e.IdVenta)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Cliente)
                .WithMany()
                .HasForeignKey(e => e.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Abonos
        modelBuilder.Entity<Abono>(entity =>
        {
            entity.ToTable("Abonos");
            entity.HasKey(e => e.IdAbono);
            entity.Property(e => e.Monto).HasPrecision(18, 2);
            entity.Property(e => e.MetodoPago).HasMaxLength(50).HasDefaultValue("Efectivo");
            entity.Property(e => e.Notas).HasMaxLength(255);
            entity.Property(e => e.FechaHora).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.CuentaCobrar)
                .WithMany(c => c.Abonos)
                .HasForeignKey(e => e.IdCuentaCobrar)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ConfiguracionNegocio
        modelBuilder.Entity<ConfiguracionNegocio>(entity =>
        {
            entity.ToTable("ConfiguracionNegocio");
            entity.HasKey(e => e.IdConfig);
            entity.Property(e => e.NombreNegocio).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Propietario).HasMaxLength(100);
            entity.Property(e => e.Telefono).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Direccion).HasMaxLength(255);
            entity.Property(e => e.Nrc).HasMaxLength(50);
            entity.Property(e => e.Nit).HasMaxLength(50);
            entity.Property(e => e.TasaIva).HasPrecision(5, 2).HasDefaultValue(13.00m);
            entity.Property(e => e.MensajeTicket).HasMaxLength(255);
            entity.Property(e => e.FelSvApiUrl).HasMaxLength(255).IsRequired(false);
            entity.Property(e => e.FelSvTenantId).HasMaxLength(100).IsRequired(false);
            entity.Property(e => e.FelSvAmbiente).HasMaxLength(10).IsRequired(false);
            entity.Property(e => e.CodEstablecimiento).HasMaxLength(10).IsRequired(false);
            entity.Property(e => e.CodPuntoVenta).HasMaxLength(10).IsRequired(false);
            entity.Property(e => e.CatDepartamento).HasMaxLength(10).IsRequired(false);
            entity.Property(e => e.CatMunicipio).HasMaxLength(10).IsRequired(false);
            entity.Property(e => e.NombreImpresora).HasMaxLength(150).IsRequired(false);
            entity.Property(e => e.NombreImpresoraDocumentos).HasMaxLength(150).IsRequired(false);
            entity.Property(e => e.AnchoPapelMm).HasDefaultValue(80);
            entity.Property(e => e.ImpresionDirecta).HasDefaultValue(true);
            entity.Property(e => e.CortarPapel).HasDefaultValue(true);
        });

        // ClienteCuentas
        modelBuilder.Entity<ClienteCuenta>(entity =>
        {
            entity.ToTable("ClienteCuentas");
            entity.HasKey(e => e.IdClienteCuenta);
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Clave).HasMaxLength(255).IsRequired();
            entity.Property(e => e.TokenRecuperacion).HasMaxLength(100);
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Cliente)
                .WithMany()
                .HasForeignKey(e => e.IdCliente)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrdenesWeb
        modelBuilder.Entity<OrdenWeb>(entity =>
        {
            entity.ToTable("OrdenesWeb");
            entity.HasKey(e => e.IdOrden);
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.EstadoPago).HasMaxLength(30).HasDefaultValue("Pendiente");
            entity.Property(e => e.EstadoEnvio).HasMaxLength(30).HasDefaultValue("Procesando");
            entity.Property(e => e.FechaOrden).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Cliente)
                .WithMany()
                .HasForeignKey(e => e.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Venta)
                .WithMany()
                .HasForeignKey(e => e.IdVenta)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // DetalleOrdenesWeb
        modelBuilder.Entity<DetalleOrdenWeb>(entity =>
        {
            entity.ToTable("DetalleOrdenesWeb");
            entity.HasKey(e => e.IdDetalleOrden);
            entity.Property(e => e.Cantidad).HasPrecision(12, 3);
            entity.Property(e => e.Precio).HasPrecision(18, 4);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);

            entity.HasOne(e => e.OrdenWeb)
                .WithMany(o => o.DetalleOrdenesWeb)
                .HasForeignKey(e => e.IdOrden)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProductoMateriales (BOM / Insumos)
        modelBuilder.Entity<ProductoMaterial>(entity =>
        {
            entity.ToTable("ProductoMateriales");
            entity.HasKey(e => e.IdProductoMaterial);
            entity.Property(e => e.Cantidad).HasPrecision(10, 2).HasDefaultValue(1.00m);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.ProductoPadre)
                .WithMany(p => p.Materiales)
                .HasForeignKey(e => e.IdProductoPadre)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ProductoInsumo)
                .WithMany()
                .HasForeignKey(e => e.IdProductoInsumo)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AjustesInventario (Auditoría Supervisada)
        modelBuilder.Entity<AjusteInventario>(entity =>
        {
            entity.ToTable("AjustesInventario");
            entity.HasKey(e => e.IdAjuste);
            entity.Property(e => e.StockAnterior).HasPrecision(12, 3);
            entity.Property(e => e.StockNuevo).HasPrecision(12, 3);
            entity.Property(e => e.Diferencia).HasPrecision(12, 3);
            entity.Property(e => e.Motivo).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FechaHora).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Producto)
                .WithMany(p => p.AjustesInventario)
                .HasForeignKey(e => e.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UsuarioSupervisor)
                .WithMany()
                .HasForeignKey(e => e.IdUsuarioSupervisor)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MovimientosCaja (Retiros, Gastos, Ingresos manuales)
        modelBuilder.Entity<MovimientoCaja>(entity =>
        {
            entity.ToTable("MovimientosCaja");
            entity.HasKey(e => e.IdMovimiento);
            entity.Property(e => e.Tipo).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Monto).HasPrecision(18, 2);
            entity.Property(e => e.Motivo).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FechaHora).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Corte)
                .WithMany()
                .HasForeignKey(e => e.IdCorte)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // CambiosPrecio
        modelBuilder.Entity<CambioPrecio>(entity =>
        {
            entity.ToTable("CambiosPrecio");
            entity.HasKey(e => e.IdCambioPrecio);
            entity.Property(e => e.CostoAnterior).HasPrecision(18, 4);
            entity.Property(e => e.CostoNuevo).HasPrecision(18, 4);
            entity.Property(e => e.PrecioVentaAnterior).HasPrecision(18, 4);
            entity.Property(e => e.PrecioVentaNuevo).HasPrecision(18, 4);
            entity.Property(e => e.MargenPorcentaje).HasPrecision(18, 2);
            entity.Property(e => e.Motivo).HasMaxLength(255);
            entity.Property(e => e.Estado).HasMaxLength(50).HasDefaultValue("Pendiente");
            entity.Property(e => e.FechaHora).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Compra)
                .WithMany()
                .HasForeignKey(e => e.IdCompra)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Mermas (Bajas de inventario por plagas, rotura/daño, vencimiento, consumo interno)
        modelBuilder.Entity<Merma>(entity =>
        {
            entity.ToTable("Mermas");
            entity.HasKey(e => e.IdMerma);
            entity.Property(e => e.Cantidad).HasPrecision(12, 3);
            entity.Property(e => e.CantidadBaseStock).HasPrecision(12, 3);
            entity.Property(e => e.CostoUnitario).HasPrecision(18, 4);
            entity.Property(e => e.CostoTotal).HasPrecision(18, 2);
            entity.Property(e => e.Motivo).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.FechaHora).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UsuarioSupervisor)
                .WithMany()
                .HasForeignKey(e => e.IdUsuarioSupervisor)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UsuarioCajero)
                .WithMany()
                .HasForeignKey(e => e.IdUsuarioCajero)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
