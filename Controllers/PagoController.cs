using Ecommerce_2024_1_NJD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;

namespace Ecommerce_2024_1_NJD.Controllers
{
    public class PagoController : Controller
    {
        public readonly IConfiguration _iconfig; //recuperar el ConnectionStrings
        public PagoController(IConfiguration iconfig)
        {
            _iconfig = iconfig;
        }

        // Método para obtener todos los pedidos de la base de datos
        // Método para obtener todos los pedidos de la base de datos
        private IEnumerable<Pedido> GetPedidosFromDatabase(int? id = null)
        {
            List<Pedido> pedidos = new List<Pedido>();

            using (SqlConnection connection = new SqlConnection(_iconfig.GetConnectionString("cadena")))
            {
                //Devolver pedidos incluso si no hay pagos asociados
                SqlCommand command = new SqlCommand("SP_PEDIDOSYPAGOS", connection);
                command.CommandType = CommandType.StoredProcedure;

                // Si id tiene un valor, agregamos el parámetro @Id
                if (id.HasValue)
                {
                    command.Parameters.AddWithValue("@IdPedido", id.Value);
                }

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Pedido pedido = new Pedido
                        {
                            idPedido = (int)reader["idPedido"],
                            estadoPedido = reader["Estado"].ToString(),
                            fechaRegistroPedido = (DateTime)reader["FechaRegistroPedido"],
                            idUsuario = (int)reader["id_usuario"],
                            dniCliente = reader["dniC"].ToString(),
                            direccionDestino = reader["direccionDestino"].ToString(),

                            idPago = reader["IdPago"] != DBNull.Value ? (int)reader["IdPago"] : 0,
                            Fecha = reader["Fecha"] != DBNull.Value ? (DateTime)reader["Fecha"] : DateTime.MinValue,
                            MetodoPago = reader["MetodoPago"] != DBNull.Value ? reader["MetodoPago"].ToString() : ""
                        };
                        // Obtener los detalles del pedido usando el procedimiento almacenado ObtenerDetallesPedidoPorId
                        pedido.detallesPedido = ObtenerDetallesPedidoPorId(pedido.idPedido);

                        pedidos.Add(pedido);
                    }
                
            }

            return pedidos;
        }

        public IActionResult RealizarPago(int? id)
        {
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            ViewData["CantidadProdCr"] = RecoverCantidadProd();

            if (id.HasValue)
            {
                //POR MEDIO DE LA URL
                IEnumerable<Pedido> pedidos = GetPedidosFromDatabase(id);
                return View(pedidos);
            }
            else
            {
                //LISTADO NORMAL-PASANDO OBJETO COMO = NULL
                IEnumerable<Pedido> pedidos = GetPedidosFromDatabase();
                return View("RealizarPago", pedidos);
            }
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


        [HttpPost]
        public IActionResult RealizarPago(int idPedido, string metodoPago)
        {
            // Obtener el ID del usuario logueado
            string idUsuario = HttpContext.Session.GetString("IdUsuario");

            // Registrar el pago
            int idPago = RegistrarPago(idPedido, metodoPago);

            // Redireccionar a alguna vista de confirmación o a donde sea apropiado
            return RedirectToAction("Portal", "Ecommerce");
        }




        // Método para registrar el pago
        public int RegistrarPago(int idPedido, string metodoPago)
        {
            int idPago = 0;
            string connectionString = _iconfig.GetConnectionString("cadena");

            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                string query = "SpRegitrarPago"; // Asegúrate de que el nombre del procedimiento almacenado sea correcto
                SqlCommand cmd = new SqlCommand(query, cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@idPedido", idPedido);
                cmd.Parameters.AddWithValue("@metodoPago", metodoPago);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    idPago = Convert.ToInt32(dr["IdPago"]);
                }

                cn.Close();
            }

            return idPago;
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
    }
}
