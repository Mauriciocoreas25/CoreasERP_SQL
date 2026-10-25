USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'Coreas_ERP')
BEGIN
    CREATE DATABASE [Coreas_ERP];
END
GO

USE [Coreas_ERP];
GO

/****** Object:  Table [dbo].[Abonos]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Abonos](
	[IdAbono] [int] IDENTITY(1,1) NOT NULL,
	[IdCuentaCobrar] [int] NOT NULL,
	[IdUsuario] [int] NOT NULL,
	[Monto] [decimal](10, 2) NOT NULL,
	[MetodoPago] [varchar](50) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[FechaHora] [datetime2](7) NOT NULL,
	[Notas] [varchar](255) COLLATE Modern_Spanish_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdAbono] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[AjustesInventario]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[AjustesInventario](
	[IdAjuste] [int] IDENTITY(1,1) NOT NULL,
	[IdProducto] [int] NOT NULL,
	[IdUsuarioSupervisor] [int] NOT NULL,
	[StockAnterior] [decimal](12, 3) NOT NULL,
	[StockNuevo] [decimal](12, 3) NOT NULL,
	[Diferencia] [decimal](12, 3) NOT NULL,
	[Motivo] [varchar](255) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[FechaHora] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdAjuste] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[CambiosPrecio]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[CambiosPrecio](
	[IdCambioPrecio] [int] IDENTITY(1,1) NOT NULL,
	[IdProducto] [int] NOT NULL,
	[IdCompra] [int] NULL,
	[CostoAnterior] [decimal](18, 4) NOT NULL,
	[CostoNuevo] [decimal](18, 4) NOT NULL,
	[PrecioVentaAnterior] [decimal](18, 4) NOT NULL,
	[PrecioVentaNuevo] [decimal](18, 4) NOT NULL,
	[MargenPorcentaje] [decimal](18, 2) NOT NULL,
	[Motivo] [nvarchar](255) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[FechaHora] [datetime] NOT NULL,
	[Estado] [nvarchar](50) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[FechaAplicacion] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdCambioPrecio] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[Categorias]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Categorias](
	[IdCategoria] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [varchar](100) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Descripcion] [varchar](255) COLLATE Modern_Spanish_CI_AS NULL,
	[Estado] [bit] NOT NULL,
	[Icono] [varchar](50) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[ImagenUrl] [varchar](500) COLLATE Modern_Spanish_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdCategoria] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[ClienteCuentas]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[ClienteCuentas](
	[IdClienteCuenta] [int] IDENTITY(1,1) NOT NULL,
	[IdCliente] [int] NOT NULL,
	[Email] [varchar](100) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Clave] [varchar](255) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[TokenRecuperacion] [varchar](100) COLLATE Modern_Spanish_CI_AS NULL,
	[Estado] [bit] NOT NULL,
	[FechaRegistro] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdClienteCuenta] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[Clientes]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Clientes](
	[IdCliente] [int] IDENTITY(1,1) NOT NULL,
	[TipoPersona] [varchar](30) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Nombre] [varchar](150) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[TipoDocumento] [varchar](30) COLLATE Modern_Spanish_CI_AS NULL,
	[NumDocumento] [varchar](50) COLLATE Modern_Spanish_CI_AS NULL,
	[Telefono] [varchar](30) COLLATE Modern_Spanish_CI_AS NULL,
	[Email] [varchar](100) COLLATE Modern_Spanish_CI_AS NULL,
	[Direccion] [varchar](255) COLLATE Modern_Spanish_CI_AS NULL,
	[Nrc] [varchar](50) COLLATE Modern_Spanish_CI_AS NULL,
	[Giro] [varchar](150) COLLATE Modern_Spanish_CI_AS NULL,
	[FechaCreacion] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdCliente] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[Compras]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Compras](
	[IdCompra] [int] IDENTITY(1,1) NOT NULL,
	[IdProveedor] [int] NOT NULL,
	[IdUsuario] [int] NOT NULL,
	[NumFactura] [varchar](50) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Total] [decimal](10, 2) NOT NULL,
	[FechaHora] [datetime2](7) NOT NULL,
	[Estado] [varchar](30) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Observaciones] [nvarchar](max) COLLATE Modern_Spanish_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdCompra] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[ConfiguracionNegocio]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[ConfiguracionNegocio](
	[IdConfig] [int] IDENTITY(1,1) NOT NULL,
	[NombreNegocio] [varchar](150) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Propietario] [varchar](100) COLLATE Modern_Spanish_CI_AS NULL,
	[Telefono] [varchar](50) COLLATE Modern_Spanish_CI_AS NULL,
	[Email] [varchar](100) COLLATE Modern_Spanish_CI_AS NULL,
	[Direccion] [varchar](255) COLLATE Modern_Spanish_CI_AS NULL,
	[Nrc] [varchar](50) COLLATE Modern_Spanish_CI_AS NULL,
	[Nit] [varchar](50) COLLATE Modern_Spanish_CI_AS NULL,
	[TasaIva] [decimal](5, 2) NOT NULL,
	[MensajeTicket] [varchar](255) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[HabilitarDte] [bit] NOT NULL,
	[FelSvApiUrl] [varchar](255) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[FelSvTenantId] [varchar](50) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[FelSvAmbiente] [varchar](10) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[CodEstablecimiento] [varchar](10) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[CodPuntoVenta] [varchar](10) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[CatDepartamento] [nvarchar](10) COLLATE Modern_Spanish_CI_AS NULL,
	[CatMunicipio] [nvarchar](10) COLLATE Modern_Spanish_CI_AS NULL,
	[NombreImpresora] [nvarchar](150) NULL,
	[NombreImpresoraDocumentos] [nvarchar](150) NULL,
	[AnchoPapelMm] [int] NOT NULL DEFAULT(80),
	[ImpresionDirecta] [bit] NOT NULL DEFAULT(1),
PRIMARY KEY CLUSTERED 
(
	[IdConfig] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[CortesCaja]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[CortesCaja](
	[IdCorte] [int] IDENTITY(1,1) NOT NULL,
	[IdUsuario] [int] NOT NULL,
	[MontoInicial] [decimal](10, 2) NOT NULL,
	[TotalVentas] [decimal](10, 2) NOT NULL,
	[MontoFinal] [decimal](10, 2) NULL,
	[Diferencia] [decimal](10, 2) NOT NULL,
	[FechaApertura] [datetime2](7) NOT NULL,
	[FechaCierre] [datetime2](7) NULL,
	[Estado] [varchar](20) COLLATE Modern_Spanish_CI_AS NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdCorte] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[MovimientosCaja] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MovimientosCaja](
	[IdMovimiento] [int] IDENTITY(1,1) NOT NULL,
	[IdCorte] [int] NULL,
	[IdUsuario] [int] NOT NULL,
	[Tipo] [varchar](30) NOT NULL,
	[Monto] [decimal](10, 2) NOT NULL,
	[Motivo] [varchar](255) NOT NULL,
	[FechaHora] [datetime2](7) NOT NULL DEFAULT(CURRENT_TIMESTAMP),
PRIMARY KEY CLUSTERED 
(
	[IdMovimiento] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[CuentasCobrar]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[CuentasCobrar](
	[IdCuentaCobrar] [int] IDENTITY(1,1) NOT NULL,
	[IdVenta] [int] NOT NULL,
	[IdCliente] [int] NOT NULL,
	[MontoTotal] [decimal](10, 2) NOT NULL,
	[SaldoPendiente] [decimal](10, 2) NOT NULL,
	[FechaEmision] [datetime2](7) NOT NULL,
	[FechaVencimiento] [datetime2](7) NULL,
	[Estado] [varchar](30) COLLATE Modern_Spanish_CI_AS NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdCuentaCobrar] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[DetalleCompras]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DetalleCompras](
	[IdDetalleCompra] [int] IDENTITY(1,1) NOT NULL,
	[IdCompra] [int] NOT NULL,
	[IdProducto] [int] NOT NULL,
	[Cantidad] [decimal](12, 3) NOT NULL,
	[PrecioCompra] [decimal](18, 4) NOT NULL,
	[Subtotal] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdDetalleCompra] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[DetalleOrdenesWeb]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DetalleOrdenesWeb](
	[IdDetalleOrden] [int] IDENTITY(1,1) NOT NULL,
	[IdOrden] [int] NOT NULL,
	[IdProducto] [int] NOT NULL,
	[Cantidad] [decimal](12, 3) NOT NULL,
	[Precio] [decimal](18, 4) NOT NULL,
	[Subtotal] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdDetalleOrden] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[DetalleVentas]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DetalleVentas](
	[IdDetalle] [int] IDENTITY(1,1) NOT NULL,
	[IdVenta] [int] NOT NULL,
	[IdProducto] [int] NOT NULL,
	[Cantidad] [decimal](12, 3) NOT NULL,
	[Precio] [decimal](18, 4) NOT NULL,
	[Descuento] [decimal](18, 2) NOT NULL,
	[Subtotal] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdDetalle] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[OrdenesWeb]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[OrdenesWeb](
	[IdOrden] [int] IDENTITY(1,1) NOT NULL,
	[IdCliente] [int] NOT NULL,
	[FechaOrden] [datetime2](7) NOT NULL,
	[Total] [decimal](10, 2) NOT NULL,
	[EstadoPago] [varchar](30) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[EstadoEnvio] [varchar](30) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[IdVenta] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdOrden] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[ProductoMateriales]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ProductoMateriales](
	[IdProductoMaterial] [int] IDENTITY(1,1) NOT NULL,
	[IdProductoPadre] [int] NOT NULL,
	[IdProductoInsumo] [int] NOT NULL,
	[Cantidad] [decimal](10, 2) NOT NULL,
	[FechaCreacion] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdProductoMaterial] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[Productos]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Productos](
	[IdProducto] [int] IDENTITY(1,1) NOT NULL,
	[IdCategoria] [int] NOT NULL,
	[Codigo] [varchar](100) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Nombre] [varchar](150) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Descripcion] [nvarchar](max) COLLATE Modern_Spanish_CI_AS NULL,
	[PrecioCompra] [decimal](18, 4) NOT NULL,
	[PrecioVenta] [decimal](18, 4) NOT NULL,
	[Stock] [decimal](12, 3) NOT NULL,
	[StockMinimo] [decimal](12, 3) NOT NULL,
	[EsGranel] [bit] NOT NULL DEFAULT(0),
	[UnidadMedida] [varchar](30) COLLATE Modern_Spanish_CI_AS NOT NULL DEFAULT('Unidad'),
	[PrecioGranel] [decimal](18, 4) NULL,
	[Imagen] [varchar](255) COLLATE Modern_Spanish_CI_AS NULL,
	[Estado] [bit] NOT NULL,
	[FechaCreacion] [datetime2](7) NOT NULL,
	[MargenExcepcion] [decimal](5, 2) NOT NULL,
	[ImagenUrl] [varchar](500) COLLATE Modern_Spanish_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdProducto] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Codigo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[Promociones]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Promociones](
	[IdPromocion] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [varchar](100) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Descripcion] [varchar](255) COLLATE Modern_Spanish_CI_AS NULL,
	[TipoDescuento] [varchar](30) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Valor] [decimal](10, 2) NOT NULL,
	[FechaInicio] [datetime2](7) NOT NULL,
	[FechaFin] [datetime2](7) NOT NULL,
	[Estado] [bit] NOT NULL,
	[IdProducto] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[IdPromocion] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[Proveedores]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Proveedores](
	[IdProveedor] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [varchar](150) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Contacto] [varchar](100) COLLATE Modern_Spanish_CI_AS NULL,
	[Telefono] [varchar](30) COLLATE Modern_Spanish_CI_AS NULL,
	[Email] [varchar](100) COLLATE Modern_Spanish_CI_AS NULL,
	[Direccion] [varchar](255) COLLATE Modern_Spanish_CI_AS NULL,
	[Nrc] [varchar](50) COLLATE Modern_Spanish_CI_AS NULL,
	[Nit] [varchar](50) COLLATE Modern_Spanish_CI_AS NULL,
	[Estado] [bit] NOT NULL,
	[FechaCreacion] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdProveedor] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[Roles]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Roles](
	[IdRol] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [varchar](50) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Descripcion] [varchar](150) COLLATE Modern_Spanish_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdRol] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[Usuarios]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Usuarios](
	[IdUsuario] [int] IDENTITY(1,1) NOT NULL,
	[IdRol] [int] NOT NULL,
	[Usuario] [varchar](50) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Nombre] [varchar](100) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Email] [varchar](100) COLLATE Modern_Spanish_CI_AS NULL,
	[Telefono] [varchar](30) COLLATE Modern_Spanish_CI_AS NULL,
	[Clave] [varchar](255) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Estado] [bit] NOT NULL,
	[FechaCreacion] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdUsuario] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Usuario] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[Ventas]    Script Date: 5/9/2026 15:19:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Ventas](
	[IdVenta] [int] IDENTITY(1,1) NOT NULL,
	[IdCliente] [int] NOT NULL,
	[IdUsuario] [int] NOT NULL,
	[TipoComprobante] [varchar](30) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[NumComprobante] [varchar](50) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[MetodoPago] [varchar](50) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[Subtotal] [decimal](10, 2) NOT NULL,
	[Impuesto] [decimal](10, 2) NOT NULL,
	[Total] [decimal](10, 2) NOT NULL,
	[PagoCon] [decimal](10, 2) NULL,
	[Cambio] [decimal](10, 2) NULL,
	[Estado] [varchar](30) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[FechaHora] [datetime2](7) NOT NULL,
	[CodigoGeneracion] [varchar](36) COLLATE Modern_Spanish_CI_AS NULL,
	[NumeroControl] [varchar](35) COLLATE Modern_Spanish_CI_AS NULL,
	[SelloRecepcion] [varchar](100) COLLATE Modern_Spanish_CI_AS NULL,
	[EstadoDte] [varchar](30) COLLATE Modern_Spanish_CI_AS NOT NULL,
	[DteId] [varchar](50) COLLATE Modern_Spanish_CI_AS NULL,
	[RespuestaHacienda] [varchar](500) COLLATE Modern_Spanish_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[IdVenta] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[Abonos] ADD  DEFAULT ('Efectivo') FOR [MetodoPago]
GO

ALTER TABLE [dbo].[Abonos] ADD  DEFAULT (getdate()) FOR [FechaHora]
GO

ALTER TABLE [dbo].[AjustesInventario] ADD  DEFAULT (getdate()) FOR [FechaHora]
GO

ALTER TABLE [dbo].[CambiosPrecio] ADD  DEFAULT (getdate()) FOR [FechaHora]
GO

ALTER TABLE [dbo].[CambiosPrecio] ADD  DEFAULT ('Pendiente') FOR [Estado]
GO

ALTER TABLE [dbo].[Categorias] ADD  DEFAULT ((1)) FOR [Estado]
GO

ALTER TABLE [dbo].[Categorias] ADD  DEFAULT ('fa-box') FOR [Icono]
GO

ALTER TABLE [dbo].[ClienteCuentas] ADD  DEFAULT ((1)) FOR [Estado]
GO

ALTER TABLE [dbo].[ClienteCuentas] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
GO

ALTER TABLE [dbo].[Clientes] ADD  DEFAULT ('Natural') FOR [TipoPersona]
GO

ALTER TABLE [dbo].[Clientes] ADD  DEFAULT ('DUI') FOR [TipoDocumento]
GO

ALTER TABLE [dbo].[Clientes] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
GO

ALTER TABLE [dbo].[Compras] ADD  DEFAULT ((0.00)) FOR [Total]
GO

ALTER TABLE [dbo].[Compras] ADD  DEFAULT (getdate()) FOR [FechaHora]
GO

ALTER TABLE [dbo].[Compras] ADD  DEFAULT ('Completada') FOR [Estado]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ('Coreas ERP') FOR [NombreNegocio]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ('7000-0000 / 2255-0000') FOR [Telefono]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ('info@coreaserp.com') FOR [Email]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ('San Salvador, El Salvador') FOR [Direccion]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ((13.00)) FOR [TasaIva]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ('Â¡Gracias por su compra! Vuelva pronto') FOR [MensajeTicket]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ((1)) FOR [HabilitarDte]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ('http://localhost:5023') FOR [FelSvApiUrl]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ('SUCURSAL-01') FOR [FelSvTenantId]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ('00') FOR [FelSvAmbiente]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ('M001') FOR [CodEstablecimiento]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ('P001') FOR [CodPuntoVenta]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ('12') FOR [CatDepartamento]
GO

ALTER TABLE [dbo].[ConfiguracionNegocio] ADD  DEFAULT ('1201') FOR [CatMunicipio]
GO

ALTER TABLE [dbo].[CortesCaja] ADD  DEFAULT ((0.00)) FOR [MontoInicial]
GO

ALTER TABLE [dbo].[CortesCaja] ADD  DEFAULT ((0.00)) FOR [TotalVentas]
GO

ALTER TABLE [dbo].[CortesCaja] ADD  DEFAULT ((0.00)) FOR [Diferencia]
GO

ALTER TABLE [dbo].[CortesCaja] ADD  DEFAULT (getdate()) FOR [FechaApertura]
GO

ALTER TABLE [dbo].[CortesCaja] ADD  DEFAULT ('Abierta') FOR [Estado]
GO

ALTER TABLE [dbo].[CuentasCobrar] ADD  DEFAULT (getdate()) FOR [FechaEmision]
GO

ALTER TABLE [dbo].[CuentasCobrar] ADD  DEFAULT ('Pendiente') FOR [Estado]
GO

ALTER TABLE [dbo].[DetalleVentas] ADD  DEFAULT ((0.00)) FOR [Descuento]
GO

ALTER TABLE [dbo].[OrdenesWeb] ADD  DEFAULT (getdate()) FOR [FechaOrden]
GO

ALTER TABLE [dbo].[OrdenesWeb] ADD  DEFAULT ('Pendiente') FOR [EstadoPago]
GO

ALTER TABLE [dbo].[OrdenesWeb] ADD  DEFAULT ('Procesando') FOR [EstadoEnvio]
GO

ALTER TABLE [dbo].[ProductoMateriales] ADD  DEFAULT ((1.00)) FOR [Cantidad]
GO

ALTER TABLE [dbo].[ProductoMateriales] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
GO

ALTER TABLE [dbo].[Productos] ADD  DEFAULT ((0.00)) FOR [PrecioCompra]
GO

ALTER TABLE [dbo].[Productos] ADD  DEFAULT ((0.00)) FOR [PrecioVenta]
GO

ALTER TABLE [dbo].[Productos] ADD  DEFAULT ((0)) FOR [Stock]
GO

ALTER TABLE [dbo].[Productos] ADD  DEFAULT ((5)) FOR [StockMinimo]
GO

ALTER TABLE [dbo].[Productos] ADD  DEFAULT ((1)) FOR [Estado]
GO

ALTER TABLE [dbo].[Productos] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
GO

ALTER TABLE [dbo].[Productos] ADD  DEFAULT ((0.00)) FOR [MargenExcepcion]
GO

ALTER TABLE [dbo].[Promociones] ADD  DEFAULT ('Porcentaje') FOR [TipoDescuento]
GO

ALTER TABLE [dbo].[Promociones] ADD  DEFAULT ((0.00)) FOR [Valor]
GO

ALTER TABLE [dbo].[Promociones] ADD  DEFAULT ((1)) FOR [Estado]
GO

ALTER TABLE [dbo].[Proveedores] ADD  DEFAULT ((1)) FOR [Estado]
GO

ALTER TABLE [dbo].[Proveedores] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
GO

ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((1)) FOR [Estado]
GO

ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
GO

ALTER TABLE [dbo].[Ventas] ADD  DEFAULT ('Ticket') FOR [TipoComprobante]
GO

ALTER TABLE [dbo].[Ventas] ADD  DEFAULT ('Efectivo') FOR [MetodoPago]
GO

ALTER TABLE [dbo].[Ventas] ADD  DEFAULT ('Completada') FOR [Estado]
GO

ALTER TABLE [dbo].[Ventas] ADD  DEFAULT (getdate()) FOR [FechaHora]
GO

ALTER TABLE [dbo].[Ventas] ADD  DEFAULT ('No_Aplica') FOR [EstadoDte]
GO

ALTER TABLE [dbo].[Abonos]  WITH CHECK ADD FOREIGN KEY([IdCuentaCobrar])
REFERENCES [dbo].[CuentasCobrar] ([IdCuentaCobrar])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Abonos]  WITH CHECK ADD FOREIGN KEY([IdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
GO

ALTER TABLE [dbo].[AjustesInventario]  WITH CHECK ADD FOREIGN KEY([IdProducto])
REFERENCES [dbo].[Productos] ([IdProducto])
GO

ALTER TABLE [dbo].[AjustesInventario]  WITH CHECK ADD FOREIGN KEY([IdUsuarioSupervisor])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
GO

ALTER TABLE [dbo].[CambiosPrecio]  WITH CHECK ADD FOREIGN KEY([IdCompra])
REFERENCES [dbo].[Compras] ([IdCompra])
GO

ALTER TABLE [dbo].[CambiosPrecio]  WITH CHECK ADD FOREIGN KEY([IdProducto])
REFERENCES [dbo].[Productos] ([IdProducto])
GO

ALTER TABLE [dbo].[ClienteCuentas]  WITH CHECK ADD FOREIGN KEY([IdCliente])
REFERENCES [dbo].[Clientes] ([IdCliente])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Compras]  WITH CHECK ADD FOREIGN KEY([IdProveedor])
REFERENCES [dbo].[Proveedores] ([IdProveedor])
GO

ALTER TABLE [dbo].[Compras]  WITH CHECK ADD FOREIGN KEY([IdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
GO

ALTER TABLE [dbo].[CortesCaja]  WITH CHECK ADD FOREIGN KEY([IdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
GO

ALTER TABLE [dbo].[CuentasCobrar]  WITH CHECK ADD FOREIGN KEY([IdCliente])
REFERENCES [dbo].[Clientes] ([IdCliente])
GO

ALTER TABLE [dbo].[CuentasCobrar]  WITH CHECK ADD FOREIGN KEY([IdVenta])
REFERENCES [dbo].[Ventas] ([IdVenta])
GO

ALTER TABLE [dbo].[DetalleCompras]  WITH CHECK ADD FOREIGN KEY([IdCompra])
REFERENCES [dbo].[Compras] ([IdCompra])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[DetalleCompras]  WITH CHECK ADD FOREIGN KEY([IdProducto])
REFERENCES [dbo].[Productos] ([IdProducto])
GO

ALTER TABLE [dbo].[DetalleOrdenesWeb]  WITH CHECK ADD FOREIGN KEY([IdOrden])
REFERENCES [dbo].[OrdenesWeb] ([IdOrden])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[DetalleOrdenesWeb]  WITH CHECK ADD FOREIGN KEY([IdProducto])
REFERENCES [dbo].[Productos] ([IdProducto])
GO

ALTER TABLE [dbo].[DetalleVentas]  WITH CHECK ADD FOREIGN KEY([IdProducto])
REFERENCES [dbo].[Productos] ([IdProducto])
GO

ALTER TABLE [dbo].[DetalleVentas]  WITH CHECK ADD FOREIGN KEY([IdVenta])
REFERENCES [dbo].[Ventas] ([IdVenta])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[OrdenesWeb]  WITH CHECK ADD FOREIGN KEY([IdCliente])
REFERENCES [dbo].[Clientes] ([IdCliente])
GO

ALTER TABLE [dbo].[OrdenesWeb]  WITH CHECK ADD FOREIGN KEY([IdVenta])
REFERENCES [dbo].[Ventas] ([IdVenta])
ON DELETE SET NULL
GO

ALTER TABLE [dbo].[ProductoMateriales]  WITH CHECK ADD FOREIGN KEY([IdProductoPadre])
REFERENCES [dbo].[Productos] ([IdProducto])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[ProductoMateriales]  WITH CHECK ADD FOREIGN KEY([IdProductoInsumo])
REFERENCES [dbo].[Productos] ([IdProducto])
GO

ALTER TABLE [dbo].[Productos]  WITH CHECK ADD FOREIGN KEY([IdCategoria])
REFERENCES [dbo].[Categorias] ([IdCategoria])
GO

ALTER TABLE [dbo].[Promociones]  WITH CHECK ADD FOREIGN KEY([IdProducto])
REFERENCES [dbo].[Productos] ([IdProducto])
GO

ALTER TABLE [dbo].[Usuarios]  WITH CHECK ADD FOREIGN KEY([IdRol])
REFERENCES [dbo].[Roles] ([IdRol])
GO

ALTER TABLE [dbo].[Ventas]  WITH CHECK ADD FOREIGN KEY([IdCliente])
REFERENCES [dbo].[Clientes] ([IdCliente])
GO

ALTER TABLE [dbo].[Ventas]  WITH CHECK ADD FOREIGN KEY([IdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
GO

ALTER TABLE [dbo].[MovimientosCaja]  WITH CHECK ADD FOREIGN KEY([IdCorte])
REFERENCES [dbo].[CortesCaja] ([IdCorte])
ON DELETE SET NULL
GO

ALTER TABLE [dbo].[MovimientosCaja]  WITH CHECK ADD FOREIGN KEY([IdUsuario])
REFERENCES [dbo].[Usuarios] ([IdUsuario])
GO


-- ===========================================================
-- DATOS INICIALES Y CONFIGURACION DEL SISTEMA (SEED DATA)
-- ===========================================================
GO

SET IDENTITY_INSERT [dbo].[Roles] ON 

GO
INSERT [dbo].[Roles] ([IdRol], [Nombre], [Descripcion]) VALUES (1, N'Administrador', N'Acceso total a todos los modulos y configuraciones')
GO
INSERT [dbo].[Roles] ([IdRol], [Nombre], [Descripcion]) VALUES (2, N'Supervisor', N'Supervision de caja, anulacion y reportes')
GO
INSERT [dbo].[Roles] ([IdRol], [Nombre], [Descripcion]) VALUES (3, N'Cajero', N'Operacion del punto de venta y turnos de caja')
GO
SET IDENTITY_INSERT [dbo].[Roles] OFF
GO
SET IDENTITY_INSERT [dbo].[Usuarios] ON 

GO
INSERT [dbo].[Usuarios] ([IdUsuario], [IdRol], [Usuario], [Nombre], [Email], [Telefono], [Clave], [Estado], [FechaCreacion]) VALUES (1, 1, N'admin', N'Administrador General', N'admin@coreaserp.com', N'7000-0000', N'admin123', 1, CAST(N'2026-09-04T14:06:11.2200000' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[Usuarios] OFF
GO
SET IDENTITY_INSERT [dbo].[ConfiguracionNegocio] ON 

GO
INSERT [dbo].[ConfiguracionNegocio] ([IdConfig], [NombreNegocio], [Propietario], [Telefono], [Email], [Direccion], [Nrc], [Nit], [TasaIva], [MensajeTicket], [HabilitarDte], [FelSvApiUrl], [FelSvTenantId], [FelSvAmbiente], [CodEstablecimiento], [CodPuntoVenta], [CatDepartamento], [CatMunicipio], [NombreImpresora], [NombreImpresoraDocumentos], [AnchoPapelMm], [ImpresionDirecta]) VALUES (1, N'Coreas ERP', N'Administración', N'7000-0000', N'info@coreaserp.com', N'San Salvador, El Salvador', N'000000-0', N'0000-000000-000-0', CAST(13.00 AS Decimal(5, 2)), N'¡Gracias por su compra! Vuelva pronto', 1, N'http://localhost:5000', N'SUCURSAL-01', N'00', N'M001', N'P001', N'06', N'14', N'', N'', 80, 1)
GO
SET IDENTITY_INSERT [dbo].[ConfiguracionNegocio] OFF
GO
SET IDENTITY_INSERT [dbo].[Categorias] ON 

GO
INSERT [dbo].[Categorias] ([IdCategoria], [Nombre], [Descripcion], [Estado], [Icono], [ImagenUrl]) VALUES (1, N'General', N'Categoría General', 1, N'fa-box', NULL)
GO
SET IDENTITY_INSERT [dbo].[Categorias] OFF
GO
SET IDENTITY_INSERT [dbo].[Clientes] ON 

GO
INSERT [dbo].[Clientes] ([IdCliente], [TipoPersona], [Nombre], [TipoDocumento], [NumDocumento], [Telefono], [Email], [Direccion], [Nrc], [Giro], [FechaCreacion]) VALUES (1, N'Natural', N'Consumidor Final', N'DUI', N'00000000-0', N'0000-0000', N'consumidorfinal@coreaserp.com', N'San Salvador', NULL, NULL, CAST(N'2026-09-04T14:06:11.2466667' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[Clientes] OFF
GO
