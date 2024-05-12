using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Session;
using Ecommerce_2024_1_NJD.Models;
using Microsoft.Win32;
using Humanizer;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce_2024_1_NJD.Controllers
{
    public class ECommerceController : Controller
    {
        public readonly IConfiguration _iconfig; //recuperar el ConnectionStrings
        public ECommerceController(IConfiguration iconfig)
        {
            _iconfig = iconfig;
        }

        IEnumerable<Producto> Catalogo()
        {
            List<Producto> temporal = new List<Producto>();
            string connectionString = _iconfig.GetConnectionString("cadena");
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                string query = @"SELECT p.IdProducto, p.NombreProducto, p.IdCategoria, c.NombreCategoria AS NombreCategoria, 
                               p.Precio, p.Stock, CONVERT(varchar, p.FechaRegistro, 103) AS FechaRegistro
                               FROM Producto p
                               INNER JOIN Categoria c ON p.IdCategoria = c.IdCategoria Where P.Estado = 'Disponible'";
                SqlCommand cmd = new SqlCommand(query, cn);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Producto producto = new Producto()
                    {
                        IdProducto = Convert.ToInt32(dr["IdProducto"]),
                        Nombre = dr["NombreProducto"].ToString(),
                        NombreCategoria = dr["NombreCategoria"].ToString(),
                        Precio = Convert.ToDecimal(dr["Precio"]),
                        Stock = Convert.ToInt32(dr["Stock"]),
                        FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                    };
                    temporal.Add(producto); // Agregar el producto a la lista temporal
                }
                cn.Close();
            }
            return temporal;
        }

        // Método para filtrar productos por categoría utilizando el stored procedure
        public IEnumerable<Producto> FiltrarProductosPorCategoria(int idCategoria)
        {
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            List<Producto> productosFiltrados = new List<Producto>();
            string connectionString = _iconfig.GetConnectionString("cadena");
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                string query = "SpFiltradoProdCate";
                SqlCommand cmd = new SqlCommand(query, cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@nombreProducto", DBNull.Value); // No se filtra por nombre de producto
                cmd.Parameters.AddWithValue("@idCategoria", idCategoria);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Producto producto = new Producto()
                    {
                        IdProducto = Convert.ToInt32(dr["IdProducto"]),
                        Nombre = dr["NombreProducto"].ToString(),
                        NombreCategoria = dr["NombreCategoria"].ToString(),
                        Precio = Convert.ToDecimal(dr["Precio"]),
                        Stock = Convert.ToInt32(dr["Stock"]),
                        FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                    };
                    productosFiltrados.Add(producto);
                }
                cn.Close();
            }
            
            return productosFiltrados;
        }

        // GET
        public IActionResult FiltrarPorCategoria(int idCategoria)
        {

            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            ViewData["CantidadProdCr"] = RecoverCantidadProd();

            string nombreCategoria = ObtenerNombreCategoria(idCategoria);
            ViewBag.NombreCategoria = nombreCategoria;

            // Si no encuentra el nombre, es porque no existe
            // En ese caso redirigir a Portal, para mostrar todo
            if (string.IsNullOrEmpty(nombreCategoria))
            {
                return RedirectToAction("Portal");
            }


            // Filtrar productos por categoría
            IEnumerable<Producto> productosFiltrados = FiltrarProductosPorCategoria(idCategoria);
            ViewBag.CategoriaID = idCategoria + ""; // Cadena vacia para manejar como string

            // Obtener cantidad de resultador
            int cantidadProductosFiltrados = productosFiltrados.Count();

            // Almacenar la cantidad de productos por categoria obtenidos
            ViewBag.CantidadPorCategoria = cantidadProductosFiltrados;

            return View("Portal", productosFiltrados); // Mostrar los productos filtrados en la misma vista del portal
        }


        // Método para obtener el nombre de la categoría
        private string ObtenerNombreCategoria(int idCategoria)
        {
            string nombreCategoria = "";
            string connectionString = _iconfig.GetConnectionString("cadena");
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                string query = "SELECT NombreCategoria FROM Categoria WHERE IdCategoria = @idCategoria";
                SqlCommand cmd = new SqlCommand(query, cn);
                cmd.Parameters.AddWithValue("@idCategoria", idCategoria);
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    nombreCategoria = result.ToString();
                }
                cn.Close();
            }
            return nombreCategoria;
        }


        public IActionResult Portal()
        {

            //espacio para definir el Session canasta, no no existe definir
            if (HttpContext.Session.GetString("canasta") == null)
            {
                HttpContext.Session.SetString("canasta",
                JsonConvert.SerializeObject(new List<Producto>()));
            }
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            ViewData["CantidadProdCr"] = RecoverCantidadProd();
            
            //enviar el catalogo a la vista
            return View(Catalogo());
        }

        // Método para buscar productos por nombre utilizando el stored procedure
        public IActionResult BuscarProductoPorNombre(string nombreProducto)
        {
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            ViewData["CantidadProdCr"] = RecoverCantidadProd();

            // Filtrar productos por nombre
            IEnumerable<Producto> productosFiltrados = FiltroBusquedaProd(nombreProducto);

            int cantidadProductosFiltrados = productosFiltrados.Count();
            ViewBag.CantidadCoincidencias = cantidadProductosFiltrados;


            if (string.IsNullOrEmpty(nombreProducto))
            {
                return RedirectToAction("Portal");
            }

            ViewBag.Busqueda = nombreProducto;

            return View("Portal", productosFiltrados); // Mostrar los productos filtrados en la misma vista del portal
        }

        // Método para buscar productos por nombre utilizando el stored procedure
        public IEnumerable<Producto> FiltroBusquedaProd(string nombreProducto)
        {
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            List<Producto> productosFiltrados = new List<Producto>();

            // MANEJO DE NULOS , PROD = 0 && 1 , TRUE
            if (!string.IsNullOrEmpty(nombreProducto))
            {
                string connectionString = _iconfig.GetConnectionString("cadena");
                using (SqlConnection cn = new SqlConnection(connectionString))
                {
                    cn.Open();
                    string query = "SpBuscarProductoPorNombre";
                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nombreProducto", nombreProducto);
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        Producto producto = new Producto()
                        {
                            IdProducto = Convert.ToInt32(dr["IdProducto"]),
                            Nombre = dr["NombreProducto"].ToString(),
                            NombreCategoria = dr["NombreCategoria"].ToString(),
                            Precio = Convert.ToDecimal(dr["Precio"]),
                            Stock = Convert.ToInt32(dr["Stock"]),
                            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                        };
                        productosFiltrados.Add(producto);
                    }
                    cn.Close();
                }
            }
 
            return productosFiltrados;
        }


        public IActionResult Seleccionar(int id = 0)
        {
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");

            ViewData["CantidadProdCr"] = RecoverCantidadProd();
            
            // Buscar producto por ID
            Producto reg = Catalogo().FirstOrDefault(p => p.IdProducto == id);
            if (reg == null)
            {
                return RedirectToAction("Portal");
            }
            else
            {
                return View(reg);
            }
        }

        [HttpPost]
        public IActionResult Seleccionar(int idproducto, int cantidad)
        {
            // Deserializar la sesión "canasta" para obtener los productos almacenados
            List<Producto> temporalProducto = JsonConvert.DeserializeObject<List<Producto>>(
                HttpContext.Session.GetString("canasta"));

            // Buscar si ya existe el producto en la canasta
            Producto productoExistente = temporalProducto.FirstOrDefault(p => p.IdProducto == idproducto);

            if (productoExistente != null)
            {
                // Si el producto ya existe en la canasta, actualizar la cantidad
                productoExistente.cantidad += cantidad;
            }
            else
            {
                // Si el producto no existe en la canasta, agregarlo
                Producto regTemp = Catalogo().FirstOrDefault(p => p.IdProducto == idproducto);
                Producto selectProd = new Producto()
                {
                    IdProducto = idproducto,
                    Nombre = regTemp.Nombre,
                    NombreCategoria = regTemp.NombreCategoria,
                    Precio = regTemp.Precio,
                    cantidad = cantidad,
                    FechaRegistro = regTemp.FechaRegistro
                };
                temporalProducto.Add(selectProd);
            }

            // Volver a serializar y almacenar la lista de productos en la sesión
            HttpContext.Session.SetString("canasta", JsonConvert.SerializeObject(temporalProducto));
            ViewBag.mensaje = "Producto agregado correctamente";
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");

            // Recuperar la cantidad de productos en la canasta y agregarla a ViewData
            ViewData["CantidadProdCr"] = RecoverCantidadProd();

            // Regresa a la vista mediante redirección y con el ID previamente proporcionado
            return RedirectToAction("Seleccionar", new { id = idproducto });
        }

        // Método privado para obtener la cantidad de productos en la canasta
        private int RecoverCantidadProd()
        {
            var canasta = HttpContext.Session.GetString("canasta");
            if (string.IsNullOrEmpty(canasta))
            {
                return 0; // Si el carrito está vacío, retorna 0 productos
            }
            else
            {
                var productosEnCarrito = JsonConvert.DeserializeObject<List<Producto>>(canasta);
                return productosEnCarrito.Sum(p => p.cantidad); // Retorna la cantidad total de productos en el carrito
            }
        }

        public IActionResult Resumen()
        {
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            ViewData["CantidadProdCr"] = RecoverCantidadProd();
            
            // Verificar si la sesión "canasta" está vacía o no existe
            var canasta = HttpContext.Session.GetString("canasta");
            if (string.IsNullOrEmpty(canasta))
            {
                // Si la sesión "canasta" está vacía o no existe, mostrar un mensaje indicando que el carrito está vacío
                ViewBag.Mensaje = "Tu carrito de compras está vacío";
                return View(new List<Producto>());
            }
            else
            {
                // Si la sesión "canasta" contiene productos, mostrar el resumen del carrito de compras
                return View(JsonConvert.DeserializeObject<List<Producto>>(canasta));
            }
        }


        public IActionResult Delete(int id, int q)
        {
            //ELIMINAR EL REGISTRO DE LA SESSION CANATA POR ID PRODUCTO Y CANTIDAD X=X X++ # X--
            List<Producto> registrosAbstract =
                JsonConvert.DeserializeObject<List<Producto>>(HttpContext.Session.GetString("canasta"));

            registrosAbstract.Remove(registrosAbstract.Find(p => p.IdProducto == id && p.cantidad == q));

            HttpContext.Session.SetString("canasta", JsonConvert.SerializeObject(registrosAbstract));
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            ViewData["CantidadProdCr"] = RecoverCantidadProd();
            
            //REDIRECCION HACIA EL RESUMEN
            return RedirectToAction("Resumen");
        }


        public IActionResult Comprar()
        {
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            ViewData["CantidadProdCr"] = RecoverCantidadProd();
           
            // Variable que muestra el contenido del JSON del carrito/canasta
            var canasta = HttpContext.Session.GetString("canasta");

            // La variable canasta puede devolver 2 valores diferentes si no tiene productos
            // Si ingresas sin iniciar sesión no devuelve nada (null), y inicias sesión, devuelve "[]"
            
            // Por si la canasta no tiene productos dentro
            if (string.IsNullOrEmpty(canasta) || canasta == "[]")
            {
                // Redirigir a la acción Resumen
                return RedirectToAction("Resumen");
            }
            else
            {
                // Mostrar vista sin problemas
                return View();
            }
        }



        [HttpPost]
        public IActionResult Comprar(UsuarioRegPedido regTemp)
        {
            string mensaje = "";
            string connectionString = _iconfig.GetConnectionString("cadena");

            // Usar la declaración "using" para asegurar que la conexión se cierre correctamente
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                // Guardar y definir transacción, actualizar stock (guardar-cabecera-detalle)
                cn.Open();
                SqlTransaction bgTransaction = cn.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    // Tabla Pedidos
                    SqlCommand cmd = new SqlCommand("usp_agrega_pedido", cn, bgTransaction);
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Indicar que la primera variable de salida es de tipo OUTPUT = idpedido (encapsulamiento)
                    cmd.Parameters.Add("@idpedido", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.AddWithValue("@idUsuario", HttpContext.Session.GetString("IdUsuario")); // Nuevo parámetro para el ID de usuario
                    cmd.Parameters.AddWithValue("@dniC", regTemp.dni);
                    cmd.Parameters.AddWithValue("@direccionDestino", regTemp.direccionDestino);

                    // Ejecutar el procedimiento almacenado para agregar el pedido
                    cmd.ExecuteNonQuery();

                    // Recuperar el código que se generó automáticamente para el pedido
                    int idpedido = (int)cmd.Parameters["@idpedido"].Value;

                    // Obtener los datos del detalle de la canasta
                    List<Producto> recuperacionTemporal = JsonConvert.DeserializeObject<List<Producto>>(
                        HttpContext.Session.GetString("canasta"));

                    // Insertar el detalle del pedido
                    foreach (Producto itProd in recuperacionTemporal)
                    {
                        cmd = new SqlCommand("exec usp_agrega_detalle @idPedido,@idProducto,@Cantidad,@Precio", cn, bgTransaction);
                        cmd.Parameters.AddWithValue("@idPedido", idpedido);
                        cmd.Parameters.AddWithValue("@idProducto", itProd.IdProducto);
                        cmd.Parameters.AddWithValue("@Cantidad", itProd.cantidad);
                        cmd.Parameters.AddWithValue("@Precio", itProd.monto);
                        cmd.ExecuteNonQuery();
                    }

                    // Actualizar el stock de los productos
                    foreach (Producto itProd in recuperacionTemporal)
                    {
                        cmd = new SqlCommand("exec usp_actualiza_stock @idproducto,@cant", cn, bgTransaction);
                        cmd.Parameters.AddWithValue("@idproducto", itProd.IdProducto);
                        cmd.Parameters.AddWithValue("@cant", itProd.cantidad);
                        cmd.ExecuteNonQuery();
                    }

                    // Confirmar la transacción
                    bgTransaction.Commit();
                    mensaje = $"Se ha registrado el pedido número {idpedido}";

                    // Limpiar la sesión "canasta" después de completar la compra
                    HttpContext.Session.Remove("canasta");

                    // Esto es para redirigir a los detalles del pedido recién registrado
                    return RedirectToAction("RealizarPago", "Pago", new { id = idpedido });
                }
                catch (SqlException ex)
                {
                    // En caso de error, hacer rollback y manejar la excepción
                    mensaje = $"Error al procesar la transacción: {ex.Message}";
                    bgTransaction.Rollback();
                }
                finally
                {
                    // Cerrar la conexión
                    cn.Close();
                }
            }
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            ViewData["CantidadProdCr"] = RecoverCantidadProd();
   
            // Asignar el mensaje a la vista
            ViewBag.mensaje = mensaje;
            return View();
        }
    }
}