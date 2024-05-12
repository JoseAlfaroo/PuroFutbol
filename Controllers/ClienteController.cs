using Ecommerce_2024_1_NJD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Ecommerce_2024_1_NJD.Controllers
{
    public class ClienteController : Controller
    {

        public readonly IConfiguration _iconfig;

        public ClienteController(IConfiguration config)
        {
            _iconfig = config;
        }


        public List<Cliente> ListadoClientes(string nombreBusqueda)
        {
            List<Cliente> clientes = new List<Cliente>();

            try
            {
                string connectionString = _iconfig.GetConnectionString("cadena");
                using (SqlConnection cn = new SqlConnection(connectionString))
                {
                    cn.Open();

                    SqlCommand command = new SqlCommand("SP_ListarClientes", cn);
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
                        Cliente cliente = new Cliente
                        {
                            Id_usuario = Convert.ToInt32(reader["id_usuario"]),
                            Nombres = reader["Nombres"].ToString(),
                            Apellidos = reader["Apellidos"].ToString(),
                            Email = reader["email"].ToString(),
                            CantidadPedidos = Convert.ToInt32(reader["cantidadPedidos"]),
                            CantidadPagos = Convert.ToInt32(reader["cantidadPagos"])
                        };

                        clientes.Add(cliente);
                    }


                }

            }
            catch (Exception ex)
            {
                // Err
                ViewBag.ErrorMessage = "Ocurrió un error al cargar la lista de clientes." + ex;
            }

            return clientes;

        }

        public IActionResult ListarClientes(string nombreBusqueda)
        {
            if (HttpContext.Session.GetString("Rol") == "Administrador" || HttpContext.Session.GetString("Rol") == "Moderador")
            {
                List<Cliente> clientes = ListadoClientes(nombreBusqueda);
                ViewData["Rol"] = HttpContext.Session.GetString("Rol");
                ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
                ViewData["FullName"] = HttpContext.Session.GetString("FullName");

                ViewBag.Clientes = clientes;

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
        public IActionResult BuscarClientes(string nombreBusqueda)
        {
            List<Cliente> clientes = ListadoClientes(nombreBusqueda);
            ViewData["Rol"] = HttpContext.Session.GetString("Rol");
            ViewBag.StrBusqueda = nombreBusqueda;
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");

            ViewBag.Clientes = clientes;

            return View("ListarClientes", clientes);
        }

    }
}
