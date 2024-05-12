using Ecommerce_2024_1_NJD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;

namespace Ecommerce_2024_1_NJD.Controllers
{
    public class PedidosController : Controller
    {
        public readonly IConfiguration _iconfig;

        public PedidosController(IConfiguration config)
        {
            _iconfig = config;
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


        public IActionResult ListadoPedidosxUsuario()
        {
            // Obtener el ID de usuario de la sesión
            string idUsuario = HttpContext.Session.GetString("IdUsuario");
            // Obtener el nombre completo de usuario de la sesión
            string fullName = HttpContext.Session.GetString("FullName");

            if (!string.IsNullOrEmpty(idUsuario) && !string.IsNullOrEmpty(fullName))
            {
                // Lista para almacenar los pedidos del usuario
                List<Pedido> pedidosUsuario = new List<Pedido>();

                // Conexión a la base de datos
                string connectionString = _iconfig.GetConnectionString("cadena");

                // Crear y ejecutar el comando SQL para obtener los pedidos del usuario
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "ListarPedidosPorUsuario";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@idUsuario", idUsuario);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        // Crear un objeto Pedido para cada fila en el resultado y agregarlo a la lista
                        Pedido pedido = new Pedido
                        {
                            idPedido = Convert.ToInt32(reader["idPedido"]),
                            estadoPedido = reader["Estado"].ToString(),
                            fechaRegistroPedido = Convert.ToDateTime(reader["FechaRegistroPedido"]),
                            dniCliente = reader["dniC"].ToString(),
                            direccionDestino = reader["direccionDestino"].ToString()
                        };

                        // Obtener los detalles del pedido usando el procedimiento almacenado ObtenerDetallesPedidoPorId
                        pedido.detallesPedido = ObtenerDetallesPedidoPorId(pedido.idPedido);

                        pedidosUsuario.Add(pedido);
                    }
                }

                // Pasar la lista de pedidos y el nombre completo del usuario a la vista para mostrarlos
                ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
                ViewData["FullName"] = HttpContext.Session.GetString("FullName");
                ViewData["CantidadProdCr"] = RecoverCantidadProd();
                return View(pedidosUsuario);
            }
            else
            {
                // Si no hay información de sesión, redirigir al usuario para iniciar sesión
                return RedirectToAction("LoginPage", "Usuario");
            }
        }

        public List<Pedido> ObtenerDetallesPedidoPorId(int idPedido)
        {
            List<Pedido> detallesPedidos = new List<Pedido>();

            string connectionString = _iconfig.GetConnectionString("cadena");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "ObtenerDetallesPedidoPorId";
                SqlCommand command = new SqlCommand(query, connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idPedido", idPedido);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Pedido detallePedido = new Pedido
                    {
                        idProducto = Convert.ToInt32(reader["IdProducto"]),
                        NombreProducto = reader["NombreProducto"].ToString(),
                        cantidad = Convert.ToInt32(reader["Cantidad"]),
                        Precio = Convert.ToDecimal(reader["Precio"])
                    };
                    detallesPedidos.Add(detallePedido);
                }
            }

            return detallesPedidos;
        }

        public List<Pedido> ListadoPedidos(string nombreBusqueda)
        {
            List<Pedido> pedidos = new List<Pedido>();

            try
            {
                string connectionString = _iconfig.GetConnectionString("cadena");
                using (SqlConnection cn = new SqlConnection(connectionString))
                {
                    cn.Open();

                    SqlCommand command = new SqlCommand("SP_ListarPedidos", cn);
                    command.CommandType = CommandType.StoredProcedure;


                    if (!string.IsNullOrEmpty(nombreBusqueda))
                    {
                        command.Parameters.AddWithValue("@NombreBusqueda", nombreBusqueda);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@NombreBusqueda", "*");
                    }


                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Pedido pedido = new Pedido
                        {
                            idPedido = Convert.ToInt32(reader["idPedido"]),
                            Cliente = reader["Cliente"].ToString(),
                            direccionDestino = reader["direccionDestino"].ToString(),
                            dniCliente = reader["dniC"].ToString(),
                            fechaRegistroPedido = Convert.ToDateTime(reader["FechaRegistroPedido"]),
                            FechaPago = reader["FechaPago"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FechaPago"]),
                            estadoPedido = reader["Estado"].ToString()
                        };

                        pedido.detallesPedido = ObtenerDetallesPedidoPorId(pedido.idPedido);

                        pedidos.Add(pedido);
                    }


                }

            }
            catch (Exception ex)
            {
                // Err
                ViewBag.ErrorMessage = "Ocurrió un error al cargar la lista de pedidos." + ex;
            }

            return pedidos;

        }

        public IActionResult ListarPedidos(string nombreBusqueda)
        {
            if (HttpContext.Session.GetString("Rol") == "Administrador" || HttpContext.Session.GetString("Rol") == "Moderador")
            {
                ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
                ViewData["FullName"] = HttpContext.Session.GetString("FullName");
                ViewData["Rol"] = HttpContext.Session.GetString("Rol");
                List<Pedido> pedidos = ListadoPedidos(nombreBusqueda);

                ViewBag.Pedidos = pedidos;

                return View();
            }

            else if (HttpContext.Session.GetString("Rol") == "Solicitante")
            {
                return RedirectToAction("AprobacionPendiente", "Usuario");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }


        }

        [HttpPost]
        public IActionResult BuscarPedidos(string nombreBusqueda)
        {

            ViewBag.StrBusqueda = nombreBusqueda;
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            ViewData["Rol"] = HttpContext.Session.GetString("Rol");
            List<Pedido> pedidos = ListadoPedidos(nombreBusqueda);

            ViewBag.Pedidos = pedidos;

            return View("ListarPedidos", pedidos);

        }
    }
}
