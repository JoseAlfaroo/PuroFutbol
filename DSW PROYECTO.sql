CREATE DATABASE BDPROYECTODSW1
GO

USE BDPROYECTODSW1
GO


--USUARIOS--
CREATE TABLE Usuarios(
  id_usuario INT PRIMARY KEY,
  username varchar(50) UNIQUE,
  pass varchar(20) not null,
  nombres varchar (50),
  apellidos varchar (50),
  Rol varchar(20) not null,
  email varchar (50) UNIQUE,
)
GO





--CREAR SP PARA EL REGISTRO DE USUARIOS--
DROP PROC IF EXISTS Sp_RegistrarUsuario
GO

CREATE OR ALTER PROCEDURE Sp_RegistrarUsuario
   @username varchar(50),
   @pass varchar(20),
   @nombres varchar(50),
   @apellidos varchar(50),
   @email varchar(50)
AS
BEGIN
    IF NOT EXISTS(SELECT 1 FROM Usuarios WHERE username = @username)
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM Usuarios WHERE email = @email)
        BEGIN
            DECLARE @LastUserID INT;
            SELECT @LastUserID = MAX(id_usuario) FROM Usuarios;

            -- Iniciar el ID de usuario en 1000 si no hay registros existentes
            IF @LastUserID IS NULL
                SET @LastUserID = 999;

            -- Incrementar el ID de usuario
            SET @LastUserID = @LastUserID + 1;

            INSERT INTO Usuarios(id_usuario, username, pass, nombres, apellidos, email, Rol)
            VALUES(@LastUserID, @username, @pass, @nombres, @apellidos, @email, 'Cliente');

            SELECT 'Usuario registrado correctamente' AS Resultado;
        END
        ELSE
        BEGIN
            SELECT 'El correo electrónico ya está en uso. Ingrese otro por favor' AS Resultado;
        END
    END
    ELSE
    BEGIN
        SELECT 'El nombre de usuario ya está en uso. Ingrese otro por favor' AS Resultado;
    END
END
GO




-- Crear la tabla Categoria
CREATE TABLE Categoria (
    idCategoria INT IDENTITY(1000,1) PRIMARY KEY,
    NombreCategoria NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(MAX)
);
GO




-- Crear la tabla Producto
CREATE TABLE Producto (
    idProducto INT IDENTITY(2000,1) PRIMARY KEY,
    NombreProducto NVARCHAR(100) NOT NULL,
    Imagen VARBINARY(MAX),
    Precio DECIMAL(10, 2) NOT NULL,
    Stock INT NOT NULL,
    FechaRegistro DATE NOT NULL Default(getdate()),
    Estado NVARCHAR(20) NOT NULL,
    idCategoria INT REFERENCES Categoria
);
GO




-- Crear la tabla Pedido
CREATE TABLE Pedido (
    idPedido INT PRIMARY KEY,
    Estado NVARCHAR(20) NOT NULL,
    FechaRegistroPedido DATE NOT NULL Default(getdate()),
    id_usuario INT REFERENCES Usuarios,
    dniC varchar(8),
    direccionDestino varchar(255)
);
GO




-- Crear la tabla DetallesPedido
CREATE TABLE DetallesPedido (
    idPedido INT REFERENCES Pedido,
    idProducto INT REFERENCES Producto,
    Cantidad INT,
    precio decimal (16,2)
);
GO



--Sp para registrar productos por categoria---
DROP PROCEDURE IF EXISTS Sp_RegistrarProducto;
GO

CREATE PROCEDURE Sp_RegistrarProducto
    @NombreProducto NVARCHAR(100),
    @IDCategoria INT,
    @Precio DECIMAL(10, 2),
    @Stock INT,
    @Imagen VARBINARY(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IDProducto INT;
    -- Insertar el producto en la tabla Producto

    INSERT INTO Producto (NombreProducto, idCategoria, Precio, Stock, FechaRegistro, Estado, Imagen)
    VALUES (@NombreProducto, @IDCategoria, @Precio, @Stock, GETDATE(), 'Disponible', @Imagen);

    -- Obtener el ID del producto recién insertado
    SET @IDProducto = SCOPE_IDENTITY();

    -- Devolver el ID del producto recién insertado
    SELECT @IDProducto AS idProducto;
END
GO



-------------------SP PARA STOCK---------------------------
--AUTOGENERAR PEDIDO X¨??*//$$

DROP FUNCTION IF EXISTS dbo.autogenera
GO

create or alter function dbo.autogenera() returns int
As
Begin 
    Declare @n int=(Select top 1 idProducto from Producto order by 1 desc)
    if(@n is null)
        Set @n=1
    else
        Set @n+=1

    return @n
End
go



---------------------
DROP PROC IF EXISTS usp_agrega_pedido
GO

CREATE OR ALTER PROCEDURE usp_agrega_pedido
    @idpedido INT OUTPUT,
    @idUsuario VARCHAR(50),
    @dniC VARCHAR(8),
    @direccionDestino VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    -- Generar el ID de pedido utilizando la función dbo.autogenera()
    SET @idpedido = dbo.autogenera();

    IF NOT EXISTS (SELECT 1 FROM Pedido)
        SET @idpedido = 3000;
    ELSE
    BEGIN
        -- Obtener el máximo valor de ID de pedido y sumar 1
        SELECT @idpedido = MAX(idPedido) + 1 FROM Pedido;
    END

    -- Insertar el pedido en la tabla Pedido
    INSERT INTO Pedido (idPedido, Estado, FechaRegistroPedido, dniC, direccionDestino, id_usuario)
    VALUES (@idpedido, 'Pendiente', GETDATE(), @dniC, @direccionDestino, @idUsuario);
END
GO



-----------------------------------
DROP PROC IF EXISTS usp_agrega_detalle
GO

create or alter procedure usp_agrega_detalle
@idPedido int,
@idProducto decimal,
@Cantidad int,
@Precio decimal(16, 2)
As
Insert DetallesPedido Values(@idPedido,@idProducto,@Cantidad,@Precio)
go



------------------------------------------
DROP PROC IF EXISTS usp_actualiza_stock
GO
create or alter proc usp_actualiza_stock
@idproducto int,
@cant smallint
As
    UPDATE Producto 
    SET Stock = Stock - @cant 
    WHERE idProducto = @idproducto;
go





--LOGIN FINAL--
DROP PROC IF EXISTS SP_LOGINV2
GO

--CREATE SP INICIAR SESSION Y RECUPERAR USUARIOS--
CREATE OR ALTER PROCEDURE SP_LOGINV2
    @UserNameOrEmail VARCHAR(50),
    @Password VARCHAR(20)
AS
BEGIN
    -- Declarar variables para almacenar los datos del usuario
    DECLARE @IdUsuario INT,
            @Username VARCHAR(50),
            @Nombres VARCHAR(100),
            @Apellidos VARCHAR(100),
            @Rol VARCHAR(20),
            @Email VARCHAR(100),
            @Pass VARCHAR(20);

    -- Verificar las credenciales del usuario y recuperar sus datos
    SELECT TOP 1 @IdUsuario = id_usuario,
                 @Username = username,
                 @Nombres = nombres,
                 @Apellidos = apellidos,
                 @Rol = Rol,
                 @Email = email,
                 @Pass = pass
    FROM Usuarios
    WHERE (username = @UserNameOrEmail OR email = @UserNameOrEmail)
          AND pass = @Password;

    -- Verificar si se encontraron datos del usuario
    IF @@ROWCOUNT > 0
    BEGIN
        -- Devolver los datos del usuario en caso de inicio de sesión exitoso
        SELECT 'INICIO DE SESION EXITOSO' AS Resultado,
               @IdUsuario AS id_usuario,
               @Username AS username,
               @Nombres AS nombres,
               @Apellidos AS apellidos,
               @Rol AS Rol,
               @Email AS email,
               @Pass AS pass,
               CONCAT(@Nombres, ' ', @Apellidos) AS FullName -- Unir nombre y apellido en una sola cadena
        ;
    END
    ELSE
    BEGIN
        -- Devolver un mensaje de error si las credenciales son incorrectas
        SELECT 'CREDENCIALES INCORRECTAS. INGRESA DE NUEVO LA CONTRASEÑA' AS Resultado,
               NULL AS id_usuario; -- Devolver NULL para el ID del usuario
    END
END;





--SP PARA VER SI SE OBTIENE LA IMAGEN POR ID- CONCATENAR IMAGEN-
--SE PUEDE UTILIZAR EN TODO EL PROYECTO EL SP--

DROP PROC IF EXISTS Sp_ObtenerImagenProducto
GO

CREATE OR ALTER  PROCEDURE Sp_ObtenerImagenProducto
    @IdProducto INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Seleccionar la imagen del producto según su ID
    SELECT Imagen
    FROM Producto
    WHERE idProducto = @IdProducto;
END




--YA QUE SOMOS ADMINISTRADOR--
--EL ADMINISTRADOR TIENE LA LIBERTAD DE CAMBIAR A LOS PRODUCTOS
--ES DECIR QUE PODRA HACER LA OPERACION CRUD
--EN POCAS PALABRAS PUES MODIFICAR LOS PRODUCTOS, EDITARLOS Y MAS COSAS SI...
--APLICAR CONCEPTO DE LA ABSTRACCION--
DROP PROC IF EXISTS SP_ActualizarProducto
GO

CREATE OR ALTER PROCEDURE SP_ActualizarProducto
    @idProducto INT,
    @NombreProducto NVARCHAR(100) = NULL,
    @Imagen VARBINARY(MAX) = NULL,
    @Precio DECIMAL(10, 2) = NULL,
    @Stock INT = NULL,
	@idCategoria INT = NULL
AS
BEGIN
    UPDATE Producto
    SET 
        NombreProducto = ISNULL(@NombreProducto, NombreProducto),
        Imagen = ISNULL(@Imagen, Imagen),
        Precio = ISNULL(@Precio, Precio),
        Stock = ISNULL(@Stock, Stock),
        FechaRegistro = GETDATE(), -- Actualizar la fecha de registro automáticamente
		idCategoria = ISNULL(@idCategoria, idCategoria)
    WHERE idProducto = @idProducto;
END;
GO





---SP ESTADO PRODUCTO--
DROP PROC IF EXISTS SP_EstadoProducto
GO

CREATE PROCEDURE SP_EstadoProducto
    @idProducto INT
AS
BEGIN
    UPDATE Producto
    SET Estado = CASE WHEN Estado = 'Disponible' THEN 'No disponible' ELSE 'Disponible' END
    WHERE idProducto = @idProducto;
END;
GO






--LISTAR PRODUCTOS--
DROP PROC IF EXISTS SP_ListarProductos
GO

CREATE OR ALTER PROCEDURE SP_ListarProductos
AS
BEGIN
    SELECT idProducto, NombreProducto, Imagen, Precio, Stock, FechaRegistro, Estado, Categoria.idCategoria, Categoria.NombreCategoria 
    FROM Producto Join Categoria
	ON Producto.idCategoria = Categoria.idCategoria
END;
GO




---SP PARA BUSCAR PRODUCTOS--
DROP PROC IF EXISTS SpBuscarProductosNombre
GO
CREATE PROCEDURE SpBuscarProductosNombre
    @Nombre NVARCHAR(100)
AS
BEGIN
    SELECT idProducto, NombreProducto, Imagen, Precio, Stock, FechaRegistro, Estado, idCategoria
    FROM Producto
    WHERE NombreProducto LIKE '%' + @Nombre + '%';
END;
GO



------------------------------------------------------------------------------
DROP PROC IF EXISTS ListarPedidosPorUsuario
go

CREATE PROCEDURE ListarPedidosPorUsuario
    @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idPedido, Estado, FechaRegistroPedido, dniC, direccionDestino
    FROM Pedido
    WHERE id_usuario = @idUsuario;
END
GO

EXEC ListarPedidosPorUsuario @idUsuario = '1003'
SELECT * FROM Pedido
go



----------------------------------------------
DROP PROC IF EXISTS ObtenerDetallesPedidoPorId
GO

CREATE OR ALTER PROCEDURE ObtenerDetallesPedidoPorId
    @idPedido INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT dp.idProducto AS IdProducto, p.NombreProducto AS NombreProducto, dp.Cantidad, dp.Precio
    FROM DetallesPedido dp
    INNER JOIN Producto p ON dp.idProducto = p.idProducto
    WHERE dp.idPedido = @idPedido;
END




--SP PARA BUSCAR PRODUCTOS POR NOMBRE--
--PARA CLIENTES
DROP PROC IF EXISTS SpBuscarProductoPorNombre
GO

CREATE OR ALTER PROCEDURE SpBuscarProductoPorNombre
    @nombreProducto NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.IdProducto, p.NombreProducto, p.IdCategoria, c.NombreCategoria AS NombreCategoria, 
           p.Precio, p.Stock, CONVERT(varchar, p.FechaRegistro, 103) AS FechaRegistro
    FROM Producto p
    INNER JOIN Categoria c ON p.IdCategoria = c.IdCategoria
    WHERE p.NombreProducto LIKE '%' + @nombreProducto + '%' AND p.Estado = 'Disponible';
END






--------SP PARA FILTRAR PRODUCTOS POR CATEGORIA--- 
--TENIENDO EN CUENTA QUE NECESITAREMOS EL  @nombreProducto , PERO ESTE SE MANEJARA COMO NULO Y ALMACENARA EN (Portal)

DROP PROC IF EXISTS SpFiltradoProdCate
go

CREATE OR ALTER PROCEDURE SpFiltradoProdCate
    @nombreProducto NVARCHAR(100) = NULL,
    @idCategoria INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.idProducto, p.NombreProducto, p.Precio, p.Stock, p.FechaRegistro, p.Estado, c.NombreCategoria
    FROM Producto p
    INNER JOIN Categoria c ON p.idCategoria = c.idCategoria
    WHERE (@nombreProducto IS NULL OR p.NombreProducto LIKE '%' + @nombreProducto + '%')
    AND (@idCategoria IS NULL OR p.idCategoria = @idCategoria) AND p.Estado = 'Disponible';
END
-----------------------------------------
DROP PROC IF EXISTS SpListadoCategorias
go



CREATE OR ALTER PROCEDURE SpListadoCategorias
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idCategoria, NombreCategoria, Descripcion
    FROM Categoria;
END;
GO



---PARA EDITAR CATEGORIAS----
DROP PROC IF EXISTS SpActualizarCategorias
go

CREATE OR ALTER PROCEDURE SpActualizarCategorias
    @idCategoria INT,
    @nombreCategoria NVARCHAR(100) = NULL,
    @descripcion NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Categoria
    SET
        NombreCategoria = ISNULL(@nombreCategoria, NombreCategoria),
        Descripcion = ISNULL(@descripcion, Descripcion)
    WHERE idCategoria = @idCategoria;
END;
GO




--------SP PARA REGISTRAR CATEGORIAS---------------
DROP PROC IF EXISTS SPRegistrarCategoria
go


CREATE PROCEDURE SPRegistrarCategoria
    @NombreCategoria NVARCHAR(100),
    @Descripcion NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Categoria (NombreCategoria, Descripcion)
    VALUES (@NombreCategoria, @Descripcion);

    SELECT SCOPE_IDENTITY() AS IdCategoria; -- Retorna el ID de la categoría recién creada
END




----------------------------------------------------------------------------

CREATE TABLE Pago (
    IdPago INT IDENTITY(100000, 1) PRIMARY KEY,
    idPedido INT REFERENCES Pedido,
    Fecha DATE,
    MetodoPago VARCHAR(20)
)
GO




---VER DATOS DEL PEDIDO---

DROP PROC IF EXISTS SP_DATOSPEDIDO
go

CREATE OR ALTER PROCEDURE SP_DATOSPEDIDO
     @idPedido INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idPedido, Estado, FechaRegistroPedido, dniC, direccionDestino
    FROM Pedido
    WHERE 
        idPedido = @idPedido;
END
GO




---REGISTRAR PAGO---

DROP PROC IF EXISTS SpRegitrarPago
go


CREATE OR ALTER PROCEDURE SpRegitrarPago
    @idPedido INT,
    @metodoPago VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @idPago INT;
    DECLARE @fecha DATE;

    SET @fecha = GETDATE();

    INSERT INTO Pago (idPedido, Fecha, MetodoPago)
    VALUES (@idPedido, @fecha, @metodoPago);

	UPDATE Pedido
	SET Estado = 'Pagado'
	Where idPedido = @idPedido;

    SET @idPago = SCOPE_IDENTITY();

    SELECT @idPago AS IdPago;
END
GO




---------------------------------------------------

DROP PROC IF EXISTS RecoverIdPedidoxIdU
go

CREATE OR ALTER PROCEDURE RecoverIdPedidoxIdU
    @idUsuario INT
AS
BEGIN
    SELECT TOP 100 idPedido
    FROM Pedido
    WHERE id_usuario = @idUsuario
    ORDER BY FechaRegistroPedido DESC;
END
GO



-----------------------------------------
DROP PROC IF EXISTS ListarPedidosPorUsuarioIdPedido
GO

CREATE OR ALTER PROCEDURE ListarPedidosPorUsuarioIdPedido
    @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 idPedido, Estado, FechaRegistroPedido, dniC, direccionDestino
    FROM Pedido
    WHERE id_usuario = @idUsuario
    ORDER BY idPedido DESC;
END
GO


SELECT * FROM Producto;
SELECT * FROM DetallesPedido;
SELECT * FROM Pedido;
SELECT* FROM Pago
GO




--SP PARA TENER EL PEDIDO Y DATOS DE PAGO

DROP PROC IF EXISTS SP_PEDIDOSYPAGOS
GO

CREATE OR ALTER PROC SP_PEDIDOSYPAGOS
@IdPedido INT = NULL
AS
BEGIN 
	SELECT PD.idPedido, PD.Estado, PD.FechaRegistroPedido, PD.dniC, PD.direccionDestino, PD.id_usuario, PG.IdPago, PG.Fecha, PG.MetodoPago FROM Pedido PD Left Join Pago PG ON PD.idPedido = PG.idPedido
	WHERE (@IdPedido IS NULL OR PD.idPedido = @IdPedido)
END
GO







-------------- ESTE ES EL SP TOP 5 VENDIDOS
DROP PROC IF EXISTS SP_TOPMASVENDIDOS
GO

CREATE OR ALTER PROC SP_TOPMASVENDIDOS
AS
BEGIN
	SELECT TOP 5 dpd.idProducto, pr.NombreProducto, SUM(dpd.precio) AS TotalIngresos, SUM(dpd.Cantidad) AS NroVentas
	FROM DetallesPedido dpd
	JOIN Producto pr ON dpd.idProducto = pr.idProducto
	JOIN Pedido pd ON dpd.idPedido = pd.idPedido
	WHERE pd.Estado = 'Pagado'
	GROUP BY dpd.idProducto, pr.NombreProducto, PD.Estado
	ORDER BY NroVentas DESC;
END
GO

------------SP CONTEOS DASHBOARD
DROP PROC IF EXISTS SP_CONTEOSDASHBOARD
GO

CREATE OR ALTER PROC SP_CONTEOSDASHBOARD
AS
BEGIN
	SELECT
    (SELECT COUNT(*) FROM Producto) AS TotalProductos,
    (SELECT COUNT(*) FROM Categoria) AS TotalCategorias,
    (SELECT COUNT(*) FROM Usuarios WHERE Rol = 'Cliente') AS TotalClientes,
    (SELECT COUNT(*) FROM Pedido) AS TotalPedidos;
END
GO



------ LIstado de pedidos para dashboard -------
DROP PROC IF EXISTS SP_ListarPedidos
GO

CREATE OR ALTER PROC SP_ListarPedidos
@NombreBusqueda NVARCHAR(100) = NULL
AS
BEGIN
	SET @NombreBusqueda = LTRIM(RTRIM(@NombreBusqueda))
	
	IF @NombreBusqueda = NULL OR @NombreBusqueda = '*'
	BEGIN
		SELECT PD.idPedido, CONCAT(U.nombres, ' ', U.apellidos) As Cliente, 
		PD.direccionDestino, PD.dniC, PD.FechaRegistroPedido, 
		PG.Fecha AS FechaPago, PD.Estado
		FROM Pedido PD
		JOIN Usuarios U ON PD.id_usuario = U.id_usuario 
		LEFT JOIN Pago PG ON PD.idPedido = PG.idPedido
	END

	ELSE
	IF ISNUMERIC(@NombreBusqueda) = 1
	BEGIN
		SELECT PD.idPedido, CONCAT(U.nombres, ' ', U.apellidos) As Cliente, 
		PD.direccionDestino, PD.dniC, PD.FechaRegistroPedido, 
		PG.Fecha AS FechaPago, PD.Estado
		FROM Pedido PD
		JOIN Usuarios U ON PD.id_usuario = U.id_usuario 
		LEFT JOIN Pago PG ON PD.idPedido = PG.idPedido
		WHERE U.id_usuario = @NombreBusqueda
	END

	ELSE
	BEGIN
		SELECT PD.idPedido, CONCAT(U.nombres, ' ', U.apellidos) As Cliente, 
		PD.direccionDestino, PD.dniC, PD.FechaRegistroPedido, 
		PG.Fecha AS FechaPago, PD.Estado
		FROM Pedido PD
		JOIN Usuarios U ON PD.id_usuario = U.id_usuario 
		LEFT JOIN Pago PG ON PD.idPedido = PG.idPedido
		WHERE CONCAT(U.Nombres, ' ', U.Apellidos) LIKE '%' + @NombreBusqueda + '%'
	END

END
GO




------ LIstado de Clientes para dashboard -------
DROP PROC IF EXISTS SP_ListarClientes
GO

CREATE OR ALTER PROC SP_ListarCLientes
@NombreBusqueda NVARCHAR(100) = NULL
AS
BEGIN
	SET @NombreBusqueda = LTRIM(RTRIM(@NombreBusqueda))

	IF @NombreBusqueda = NULL OR @NombreBusqueda = '*'
	BEGIN
		SELECT U.id_usuario, U.nombres, U.apellidos, U.email, COUNT(DISTINCT P.idPedido) AS cantidadPedidos,  COUNT(DISTINCT PG.IdPago) AS cantidadPagos
		FROM Usuarios U
		LEFT JOIN Pedido P ON U.id_usuario = P.id_usuario
		LEFT JOIN Pago PG on P.idPedido = PG.idPedido
		WHERE U.Rol = 'Cliente'
		GROUP BY U.id_usuario, U.nombres, U.apellidos, U.email
	END
	ELSE
	BEGIN
		SELECT U.id_usuario, U.nombres, U.apellidos, U.email, COUNT(DISTINCT P.idPedido) AS cantidadPedidos,  COUNT(DISTINCT PG.IdPago) AS cantidadPagos
		FROM Usuarios U
		LEFT JOIN Pedido P ON U.id_usuario = P.id_usuario
		LEFT JOIN Pago PG on P.idPedido = PG.idPedido
		WHERE U.Rol = 'Cliente' AND CONCAT(U.Nombres, ' ', U.Apellidos) LIKE '%' + @NombreBusqueda + '%'
		GROUP BY U.id_usuario, U.nombres, U.apellidos, U.email
	END
END
GO



/******* Probando (21/04/2024) ********/
--------- SOLICITUD ROL ADMIN

DROP TABLE SOLICITUD_ADMIN
GO

CREATE TABLE SOLICITUD_ADMIN(
	IdSolicitud INT IDENTITY(5000,1),
	id_usuario int references Usuarios UNIQUE,
	FechaRegSolicitud DATE NOT NULL Default(getdate()),
	FechaAprobacion DATE NULL,
	EstadoSolicitud VARCHAR(30)
)
GO




/*** SP PARA SOLICITAR PERMISOS TRAS REGISTRO ***/
DROP PROC IF EXISTS SP_RegistroYSolicitudAdmin
GO


CREATE OR ALTER PROCEDURE SP_RegistroYSolicitudAdmin
   @username varchar(50),
   @pass varchar(20),
   @nombres varchar(50),
   @apellidos varchar(50),
   @email varchar(50)
AS
BEGIN
    IF NOT EXISTS(SELECT 1 FROM Usuarios WHERE username = @username)
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM Usuarios WHERE email = @email)
        BEGIN
            DECLARE @LastUserID INT;
            SELECT @LastUserID = MAX(id_usuario) FROM Usuarios;

            -- Iniciar el ID de usuario en 1000 si no hay registros existentes
            IF @LastUserID IS NULL
                SET @LastUserID = 999;

            -- Incrementar el ID de usuario
            SET @LastUserID = @LastUserID + 1;

            INSERT INTO Usuarios(id_usuario, username, pass, nombres, apellidos, email, Rol)
            VALUES(@LastUserID, @username, @pass, @nombres, @apellidos, @email, 'Solicitante');

            INSERT INTO SOLICITUD_ADMIN(id_usuario, FechaRegSolicitud, EstadoSolicitud)
            VALUES(@LastUserID, GETDATE(), 'Pendiente');

            SELECT 'Usuario registrado correctamente' AS Resultado;
        END
        ELSE
        BEGIN
            SELECT 'El correo electrónico ya está en uso. Ingrese otro por favor' AS Resultado;
        END
    END
    ELSE
    BEGIN
        SELECT 'El nombre de usuario ya está en uso. Ingrese otro por favor' AS Resultado;
    END
END
GO





/*** SP PARA LISTAR SOLICITUDES/USUARIOS ***/

DROP PROC IF EXISTS SP_ListarSolicitantes
GO

CREATE OR ALTER PROCEDURE SP_ListarSolicitantes
AS
BEGIN
	SELECT SA.IdSolicitud, CONCAT(LTRIM(RTRIM(U.nombres)), ' ', LTRIM(RTRIM(U.apellidos))) AS Nombres, U.email, SA.FechaRegSolicitud, SA.FechaAprobacion, SA.EstadoSolicitud, U.id_usuario
	FROM Usuarios U
	JOIN SOLICITUD_ADMIN SA ON U.id_usuario = SA.id_usuario
END
GO




/******** Verificar sesión ********/
-- Verifica sesión y agrega usuario solicitante como moderador --
DROP PROC IF EXISTS SP_AGREGARADMINISTRADOR
GO

CREATE OR ALTER PROCEDURE SP_AGREGARADMINISTRADOR
@IdUsuario INT,
@Password VARCHAR(20),
@IdSolicitante INT
AS
BEGIN

	DECLARE @ResultadosIdSolicitante INT
	SELECT @ResultadosIdSolicitante = COUNT(*) FROM Usuarios WHERE id_usuario = @IdSolicitante AND rol = 'Solicitante'
    
	IF @ResultadosIdSolicitante > 0
	BEGIN
		
		DECLARE @ResultadosAutenticacion INT
		SELECT @ResultadosAutenticacion = COUNT(*) FROM Usuarios Where pass = @Password AND id_usuario = @IdUsuario AND Rol = 'Administrador'

		IF @ResultadosAutenticacion > 0
		BEGIN

			DECLARE @IdSolicitud INT
			SELECT @IdSolicitud = IdSolicitud FROM SOLICITUD_ADMIN WHERE id_usuario = @IdSolicitante;

			UPDATE Usuarios
			SET Rol = 'Moderador'
			where id_usuario = @IdSolicitante

			Update SOLICITUD_ADMIN
			Set EstadoSolicitud = 'Aprobada', 
			FechaAprobacion = GETDATE()
			Where IdSolicitud = @IdSolicitud

			PRINT 'HUGE SUCCESS'
		END
		ELSE
		THROW 50000, 'Contraseña incorrecta', 1;
	END
	ELSE
	THROW 50000, 'No se ha encontrado al solicitante', 1;
END
GO



/*********** BUSCAR USU POR EMAIL ************/
DROP PROC IF EXISTS RecuperarContraseña
GO



CREATE OR ALTER PROC RecuperarContraseña
    @Email NVARCHAR(100)
AS
BEGIN
    SELECT TOP 1 *
    FROM Usuarios
    WHERE Email = @Email
END
GO



/***** Cambiar Contraseña *****/
DROP PROC IF EXISTS CambiarContraseña
GO

CREATE OR ALTER PROC CambiarContraseña
    @UsuarioID Varchar(10),
	@Email Varchar(100),
	@NuevaPW Varchar(20)
AS
BEGIN
	UPDATE Usuarios
	SET pass = @NuevaPW
    WHERE Email = @Email AND @UsuarioID = id_usuario
END
GO

EXEC CambiarContraseña '1002', 'already.5.35@gmail.com', 'ef797c8118f02dfb6496'

SELECT * FROM Usuarios
GO

SELECT * FROM SOLICITUD_ADMIN
GO


-------------------VIEWS


CREATE VIEW VistaCompleta AS
	SELECT 
		u.id_usuario,
		u.username,
		CONCAT(u.nombres, ' ', u.apellidos) AS FullName,
		u.Rol,
		u.email,
		p.idProducto,
		p.NombreProducto,
		p.Precio,
		p.Stock,
		p.FechaRegistro,
		p.Estado AS EstadoProducto,
		c.idCategoria,
		c.NombreCategoria,
		c.Descripcion AS DescripcionCategoria,
		ped.idPedido,
		ped.Estado AS EstadoPedido,
		ped.FechaRegistroPedido,
		ped.dniC,
		ped.direccionDestino,
		dp.Cantidad AS CantidadDetalles,
		dp.precio AS PrecioDetalles,
		pa.IdPago,
		pa.Fecha AS FechaPago,
		pa.MetodoPago
	FROM Usuarios u
	JOIN Pedido ped ON u.id_usuario = ped.id_usuario
	LEFT JOIN DetallesPedido dp ON ped.idPedido = dp.idPedido
	LEFT JOIN Producto p ON dp.idProducto = p.idProducto
	LEFT JOIN Categoria c ON p.idCategoria = c.idCategoria
	LEFT JOIN Pago pa ON ped.idPedido = pa.idPedido;
GO


CREATE VIEW VistaTopMasVendidos AS
	SELECT TOP 5 dpd.idProducto, 
		pr.NombreProducto, 
		SUM(dpd.precio) AS TotalIngresos, 
		SUM(dpd.Cantidad) AS NroVentas
	FROM DetallesPedido dpd
	JOIN Producto pr ON dpd.idProducto = pr.idProducto
	JOIN Pedido pd ON dpd.idPedido = pd.idPedido
	WHERE pd.Estado = 'Pagado'
	GROUP BY dpd.idProducto, pr.NombreProducto
	ORDER BY NroVentas DESC;
GO



-- I N S E R C I O N E S
-- (Por cuestiones de practicidad, todas las contraseñas encriptadas son '12345678')

/* INSERCION ADMIN */
EXECUTE Sp_RegistrarUsuario 'JuanSanchezAdmin', 'ef797c8118f02dfb6496', 'Juan', 'Sanchez', 'juansanchez@gmail.com'
GO

UPDATE Usuarios SET Rol = 'Administrador' Where username = 'JuanSanchezAdmin'
GO



/* INSERCION MODERADOR */
EXECUTE Sp_RegistrarUsuario 'PanchoSimbronMod', 'ef797c8118f02dfb6496', 'Francisco', 'Simbron', 'franciscosimbron@gmail.com'
GO

UPDATE Usuarios SET Rol = 'Moderador' Where username = 'PanchoSimbronMod'
GO


/* INSERCION SOLICITANTE */
EXECUTE SP_RegistroYSolicitudAdmin 'CarlaLopezSol', 'ef797c8118f02dfb6496', 'Carla', 'Lopez', 'carlalopez@gmail.com'
GO



/* INSERCION CLIENTES */
EXECUTE Sp_RegistrarUsuario 'JuanRodriguez', 'ef797c8118f02dfb6496', 'Juan', 'Rodriguez', 'juanrodriguez@gmail.com'
GO

EXECUTE Sp_RegistrarUsuario 'MariaGomez', 'ef797c8118f02dfb6496', 'María', 'Gómez', 'mariagomez@gmail.com'
GO

EXECUTE Sp_RegistrarUsuario 'CarlosMartinez', 'ef797c8118f02dfb6496', 'Carlos', 'Martínez', 'carlosmartinez@gmail.com'
GO

EXECUTE Sp_RegistrarUsuario 'AnaRodriguez', 'ef797c8118f02dfb6496', 'Ana', 'Rodríguez', 'anarodriguez@gmail.com'
GO

EXECUTE Sp_RegistrarUsuario 'PedroLopez', 'ef797c8118f02dfb6496', 'Pedro', 'López', 'pedrolopez@gmail.com'
GO


SELECT * FROM Usuarios