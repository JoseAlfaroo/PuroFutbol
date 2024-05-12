using Ecommerce_2024_1_NJD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Ecommerce_2024_1_NJD.Controllers
{
    public class DashboardController : Controller
    {

        public readonly IConfiguration _iconfig;

        public DashboardController(IConfiguration config)
        {
            _iconfig = config;
        }


        public List<Producto> TopProductosVendidos()
        {
            List<Producto> ListadoProductosMasVendidos = new List<Producto>();

            string connectionString = _iconfig.GetConnectionString("cadena");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SP_TOPMASVENDIDOS";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Producto masVendidos = new Producto
                    {
                        IdProducto = Convert.ToInt32(reader["idProducto"]),
                        Nombre = reader["NombreProducto"].ToString(),
                        TotalIngresos = Convert.ToDecimal(reader["TotalIngresos"]),
                        NroVentas = Convert.ToInt32(reader["NroVentas"])

                    };
                    ListadoProductosMasVendidos.Add(masVendidos);
                }
            }
            return ListadoProductosMasVendidos;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Rol") == "Administrador" || HttpContext.Session.GetString("Rol") == "Moderador")
            {
                // Conteos
                int totalProductos = 0;
                int totalCategorias = 0;
                int totalClientes = 0;
                int totalPedidos = 0;

                string connectionString = _iconfig.GetConnectionString("cadena");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "SP_CONTEOSDASHBOARD";
                    SqlCommand command = new SqlCommand(query, connection);

                    try
                    {
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            totalProductos = Convert.ToInt32(reader["TotalProductos"]);
                            totalCategorias = Convert.ToInt32(reader["TotalCategorias"]);
                            totalClientes = Convert.ToInt32(reader["TotalClientes"]);
                            totalPedidos = Convert.ToInt32(reader["TotalPedidos"]);
                        }

                        reader.Close();
                    }
                    catch (Exception ex)
                    {
                        // Pa la prox
                    }

                }

                ViewBag.TotalProductos = totalProductos;
                ViewBag.TotalCategorias = totalCategorias;
                ViewBag.TotalClientes = totalClientes;
                ViewBag.TotalPedidos = totalPedidos;


                // Listado de los 5 por si los necesito
                ViewBag.ListadoProductosMasVendidos = TopProductosVendidos();

                if (ViewBag.ListadoProductosMasVendidos != null)
                {
                    // Arrays específicos para llenar el gráfico pie del top vendidos
                    var nombresProductos = new List<string>();
                    var nrosVentas = new List<int>();

                    foreach (var producto in ViewBag.ListadoProductosMasVendidos)
                    {
                        nombresProductos.Add(producto.Nombre);
                        nrosVentas.Add(producto.NroVentas);
                    }

                    // Pasar arrays a la vista para usarlos en el script del gráfico
                    ViewBag.NombresProductos = nombresProductos;
                    ViewBag.NrosVentas = nrosVentas;
                }



                ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
                ViewData["FullName"] = HttpContext.Session.GetString("FullName");
                ViewData["Rol"] = HttpContext.Session.GetString("Rol");
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
    }
}
