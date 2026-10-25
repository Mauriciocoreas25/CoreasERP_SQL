USE [Coreas_ERP];
GO

-- =========================================================================================
-- MIGRACIÓN DE PRECISIÓN DE DECIMALES - REGLAS MINISTERIO DE HACIENDA (EL SALVADOR)
-- Precios unitarios y costos: DECIMAL(18, 4) (evita pérdida de centavos en fracciones y CCF)
-- Totales, subtotales, impuestos, abonos y caja: DECIMAL(18, 2)
-- Stock y unidades físicas: DECIMAL(12, 3) (se mantiene intacto)
-- =========================================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

PRINT 'Actualizando precisión en tabla [Productos]...';
ALTER TABLE [dbo].[Productos] ALTER COLUMN [PrecioCompra] DECIMAL(18, 4) NOT NULL;
ALTER TABLE [dbo].[Productos] ALTER COLUMN [PrecioVenta] DECIMAL(18, 4) NOT NULL;
ALTER TABLE [dbo].[Productos] ALTER COLUMN [PrecioGranel] DECIMAL(18, 4) NULL;
ALTER TABLE [dbo].[Productos] ALTER COLUMN [CostoGranel] DECIMAL(18, 4) NULL;

PRINT 'Actualizando precisión en tabla [DetalleVentas]...';
ALTER TABLE [dbo].[DetalleVentas] ALTER COLUMN [Precio] DECIMAL(18, 4) NOT NULL;
ALTER TABLE [dbo].[DetalleVentas] ALTER COLUMN [Descuento] DECIMAL(18, 2) NOT NULL;
ALTER TABLE [dbo].[DetalleVentas] ALTER COLUMN [Subtotal] DECIMAL(18, 2) NOT NULL;

PRINT 'Actualizando precisión en tabla [Ventas]...';
ALTER TABLE [dbo].[Ventas] ALTER COLUMN [Subtotal] DECIMAL(18, 2) NOT NULL;
ALTER TABLE [dbo].[Ventas] ALTER COLUMN [Impuesto] DECIMAL(18, 2) NOT NULL;
ALTER TABLE [dbo].[Ventas] ALTER COLUMN [Total] DECIMAL(18, 2) NOT NULL;
ALTER TABLE [dbo].[Ventas] ALTER COLUMN [PagoCon] DECIMAL(18, 2) NULL;
ALTER TABLE [dbo].[Ventas] ALTER COLUMN [Cambio] DECIMAL(18, 2) NULL;

PRINT 'Actualizando precisión en tabla [CortesCaja]...';
ALTER TABLE [dbo].[CortesCaja] ALTER COLUMN [MontoInicial] DECIMAL(18, 2) NOT NULL;
ALTER TABLE [dbo].[CortesCaja] ALTER COLUMN [TotalVentas] DECIMAL(18, 2) NOT NULL;
ALTER TABLE [dbo].[CortesCaja] ALTER COLUMN [MontoFinal] DECIMAL(18, 2) NULL;
ALTER TABLE [dbo].[CortesCaja] ALTER COLUMN [Diferencia] DECIMAL(18, 2) NOT NULL;

PRINT 'Actualizando precisión en tabla [Compras]...';
ALTER TABLE [dbo].[Compras] ALTER COLUMN [Total] DECIMAL(18, 2) NOT NULL;

PRINT 'Actualizando precisión en tabla [DetalleCompras]...';
ALTER TABLE [dbo].[DetalleCompras] ALTER COLUMN [PrecioCompra] DECIMAL(18, 4) NOT NULL;
ALTER TABLE [dbo].[DetalleCompras] ALTER COLUMN [Subtotal] DECIMAL(18, 2) NOT NULL;

PRINT 'Actualizando precisión en tabla [CuentasCobrar]...';
ALTER TABLE [dbo].[CuentasCobrar] ALTER COLUMN [MontoTotal] DECIMAL(18, 2) NOT NULL;
ALTER TABLE [dbo].[CuentasCobrar] ALTER COLUMN [SaldoPendiente] DECIMAL(18, 2) NOT NULL;

PRINT 'Actualizando precisión en tabla [Abonos]...';
ALTER TABLE [dbo].[Abonos] ALTER COLUMN [Monto] DECIMAL(18, 2) NOT NULL;

PRINT 'Actualizando precisión en tabla [MovimientosCaja]...';
ALTER TABLE [dbo].[MovimientosCaja] ALTER COLUMN [Monto] DECIMAL(18, 2) NOT NULL;

PRINT 'Actualizando precisión en tabla [CambiosPrecio]...';
ALTER TABLE [dbo].[CambiosPrecio] ALTER COLUMN [CostoAnterior] DECIMAL(18, 4) NOT NULL;
ALTER TABLE [dbo].[CambiosPrecio] ALTER COLUMN [CostoNuevo] DECIMAL(18, 4) NOT NULL;
ALTER TABLE [dbo].[CambiosPrecio] ALTER COLUMN [PrecioVentaAnterior] DECIMAL(18, 4) NOT NULL;
ALTER TABLE [dbo].[CambiosPrecio] ALTER COLUMN [PrecioVentaNuevo] DECIMAL(18, 4) NOT NULL;
ALTER TABLE [dbo].[CambiosPrecio] ALTER COLUMN [MargenPorcentaje] DECIMAL(18, 2) NOT NULL;

PRINT 'Actualizando precisión en tabla [Mermas]...';
ALTER TABLE [dbo].[Mermas] ALTER COLUMN [CostoUnitario] DECIMAL(18, 4) NOT NULL;
ALTER TABLE [dbo].[Mermas] ALTER COLUMN [CostoTotal] DECIMAL(18, 2) NOT NULL;

IF OBJECT_ID(N'[dbo].[DetalleOrdenesWeb]', N'U') IS NOT NULL
BEGIN
    PRINT 'Actualizando precisión en tabla [DetalleOrdenesWeb]...';
    ALTER TABLE [dbo].[DetalleOrdenesWeb] ALTER COLUMN [Precio] DECIMAL(18, 4) NOT NULL;
    ALTER TABLE [dbo].[DetalleOrdenesWeb] ALTER COLUMN [Subtotal] DECIMAL(18, 2) NOT NULL;
END

COMMIT TRANSACTION;
PRINT '¡Migración de precisión completada exitosamente sin pérdidas de centavos!';
GO
