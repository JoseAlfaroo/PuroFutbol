using Ecommerce_2024_1_NJD.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using NuGet.Protocol.Plugins;
using System.Data;
using System.Globalization;

namespace Ecommerce_2024_1_NJD.Controllers
{

    public class ProductoController : Controller
    {
        // Instanciar conexión SQL
        public readonly IConfiguration _config;
        public readonly IWebHostEnvironment _hostEnvironment;



        public ProductoController(IConfiguration config, IWebHostEnvironment hostEnvironment)
        {
            _config = config;
            _hostEnvironment = hostEnvironment;
        }
        //GET
        public IActionResult RegistrarProducto()
        {
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            ViewData["Rol"] = HttpContext.Session.GetString("Rol");
            // Obtener la lista de categorías
            //-------------------------------------------------------------------------------------------//
            List<Producto> categorias = ObtenerCategorias();
            SelectList categoriasSelectList = new SelectList(categorias, "IdCategoria", "NombreCategoria");
            //-------------------------------------------------------------------------------------------//
            ViewData["Categorias"] = categoriasSelectList;

            if (HttpContext.Session.GetString("Rol") == "Administrador" || HttpContext.Session.GetString("Rol") == "Moderador")
            {
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



        // POST: Registrar un nuevo producto
        [HttpPost]
        public async Task<IActionResult> RegistrarProducto(IFormFile Imagen, Producto model)
        {
            try
            {
                if (Imagen != null && Imagen.Length > 0)
                {
                    //INTECTAR Y GUARDAR IMG EN MEMORIA SERVIDOR
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "ImagenesProducto");
                    var fileName = Guid.NewGuid().ToString() + "_" + Imagen.FileName;
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Imagen.CopyToAsync(stream);
                    }

                    string connectionString = _config.GetConnectionString("cadena");
                    using (SqlConnection cn = new SqlConnection(connectionString))
                    {
                        cn.Open();
                        using (SqlCommand command = new SqlCommand("Sp_RegistrarProducto", cn))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@NombreProducto", model.Nombre);
                            command.Parameters.AddWithValue("@IDCategoria", model.IdCategoria);
                            command.Parameters.AddWithValue("@Precio", model.Precio);
                            command.Parameters.AddWithValue("@Stock", model.Stock);
                            command.Parameters.AddWithValue("@Imagen", ConvertImageToByteArray(filePath)); // GUARADNDO IMAGEN COMO ARREGLO (BYTES)
                            var idProducto = command.ExecuteScalar();
                            if (idProducto != null)
                            {
                                ViewBag.Message = "Producto registrado exitosamente con ID: " + idProducto.ToString();
                            }
                            else
                            {
                                ViewBag.Error = "Ocurrió un error al registrar el producto.";
                            }
                        }
                    }
                }
                else
                {
                    // MANEJO CUANDO NO SE SUBE IMAGEN
                    ModelState.AddModelError("Imagen", "Por favor, seleccione una imagen.");
                    return View(model);
                }

                return RedirectToAction("RegistrarProducto");
            }
            catch (Exception ex)
            {
                // MANEJO EX CUANDO HAY ERROE EN EL SQL - SP
                ViewBag.ErrorEx = "Se produjo un error al procesar tu solicitud. Por favor, inténtalo de nuevo más tarde.";

                return View(model);
            }
        }

        public List<Producto> ObtenerCategorias()
        {
            string connectionString = _config.GetConnectionString("cadena");
            List<Producto> categorias = new List<Producto>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT idCategoria, NombreCategoria FROM Categoria";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Producto categoria = new Producto
                    {
                        IdCategoria = Convert.ToInt32(reader["idCategoria"]),
                        NombreCategoria = reader["NombreCategoria"].ToString()
                    };
                    categorias.Add(categoria);
                }
            }

            return categorias;
        }



        // Método para convertir la imagen a un array de bytes
        private byte[] ConvertImageToByteArray(string imagePath)
        {
            byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
            return imageBytes;
        }

        public IActionResult ObtenerImagen(int id)
        {
            string connectionString = _config.GetConnectionString("cadena");
            byte[] imageBytes;

            // Consulta para obtener la imagen del producto según su ID
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (SqlCommand command = new SqlCommand("Sp_ObtenerImagenProducto", cn))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IdProducto", id);

                    // Ejecutar el comando para obtener la imagen
                    imageBytes = (byte[])command.ExecuteScalar();
                }
            }
            if (imageBytes != null && imageBytes.Length > 0)
            {
                // Retornar la imagen como un archivo de imagen al navegador
                return File(imageBytes, "image/png"); // Suponiendo que las imágenes son PNG
            }
            else
            {
                // Si los bytes de la imagen son nulos o están vacíos, puedes manejar el error de alguna manera
                return NotFound(); // Por ejemplo, puedes retornar un error 404
            }

        }
        [HttpPost]
        public IActionResult ActualizarProducto(int idProducto, string nombre, IFormFile Imagen, decimal precio, int stock, int idCategoria)
        {
            var model = new Producto
            {
                IdProducto = idProducto,
                Nombre = nombre,
                Imagen = Imagen,
                Precio = precio,
                Stock = stock,
                IdCategoria = idCategoria,
            };

            var success = ActualizarProductoDatos(model);
            return Json(new { success });
        }

        public async Task<bool> ActualizarProductoDatos(Producto model)
        {
            try
            {
                string connectionString = _config.GetConnectionString("cadena");
                using (SqlConnection cn = new SqlConnection(connectionString))
                {
                    cn.Open();
                    using (SqlCommand command = new SqlCommand("SP_ActualizarProducto", cn))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@idProducto", model.IdProducto);

                        if (!string.IsNullOrEmpty(model.Nombre))
                            command.Parameters.AddWithValue("@NombreProducto", model.Nombre);

                        if (model.Imagen != null)
                        {
                            // INTEGRAR Y GUARDAR IMAGEN EN EL SERVIDOR
                            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "ImagenesProducto");
                            var fileName = Guid.NewGuid().ToString() + "_" + model.Imagen.FileName;
                            var filePath = Path.Combine(uploadsFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await model.Imagen.CopyToAsync(stream);
                            }

                            // Guardar imagen como arreglo de bytes
                            byte[] imageBytes = ConvertImageToByteArray(filePath);
                            command.Parameters.AddWithValue("@Imagen", imageBytes);
                        }

                        if (model.Precio != null)
                            command.Parameters.AddWithValue("@Precio", model.Precio);

                        if (model.Stock != null)
                            command.Parameters.AddWithValue("@Stock", model.Stock);

                        if (model.IdCategoria != null)
                            command.Parameters.AddWithValue("@idCategoria", model.IdCategoria);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            ViewBag.Message = "Producto actualizado correctamente";
                            return true;
                        }
                        else
                        {
                            ViewBag.Message = "Error al actualizar el producto";
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejar excepciones cuando hay errores en el SQL o el SP
                ViewBag.Message = "Error: " + ex.Message;
                return false;
            }
        }

        public IActionResult EstadoProducto(int idProducto)
        {
            try
            {
                string connectionString = _config.GetConnectionString("cadena");
                using (SqlConnection cn = new SqlConnection(connectionString))
                {
                    cn.Open();
                    SqlCommand command = new SqlCommand("SP_EstadoProducto", cn);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@idProducto", idProducto);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        // Obtener el nuevo estado del producto
                        string nuevoEstado = ObtenerEstadoProducto(idProducto);
                        return Json(new { success = true, estado = nuevoEstado });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Error al actualizar el estado del producto." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Método para obtener el estado actual del producto
        private string ObtenerEstadoProducto(int idProducto)
        {
            string connectionString = _config.GetConnectionString("cadena");
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand command = new SqlCommand("SELECT Estado FROM Producto WHERE idProducto = @idProducto", cn);
                command.Parameters.AddWithValue("@idProducto", idProducto);
                return command.ExecuteScalar()?.ToString();
            }
        }


        public IActionResult ListarProductos()
        {

            if (HttpContext.Session.GetString("Rol") == "Administrador" || HttpContext.Session.GetString("Rol") == "Moderador")
            {
                // Obtener la lista de categorías
                List<Producto> categorias = ObtenerCategorias();
                SelectList categoriasSelectList = new SelectList(categorias, "IdCategoria", "NombreCategoria");
                ViewData["Categorias"] = categoriasSelectList;

                List<Producto> productos = new List<Producto>();

                try
                {
                    string connectionString = _config.GetConnectionString("cadena");
                    using (SqlConnection cn = new SqlConnection(connectionString))
                    {
                        cn.Open();

                        SqlCommand command = new SqlCommand("SP_ListarProductos", cn);
                        command.CommandType = CommandType.StoredProcedure;

                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            Producto producto = new Producto
                            {
                                IdProducto = Convert.ToInt32(reader["idProducto"]),
                                Nombre = reader["NombreProducto"].ToString(),
                                Precio = Convert.ToDecimal(reader["Precio"]),
                                Stock = Convert.ToInt32(reader["Stock"]),
                                Estado = reader["Estado"].ToString(),
                                IdCategoria = Convert.ToInt32(reader["idCategoria"]),
                                NombreCategoria = reader["NombreCategoria"].ToString(),
                            };

                            productos.Add(producto);
                        }

                        // Iterar sobre los resultados del lector
                        while (reader.Read())
                        {
                            // Obtener el ID del producto de la fila actual y almacenarlo en la sesión
                            HttpContext.Session.SetString("IdProducto", reader["IdProducto"].ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Manejar cualquier excepción aquí
                    ViewBag.ErrorMessage = "Ocurrió un error al cargar la lista de productos.";
                }

                var viewModel = new ProductosYCategorias
                {
                    Productos = productos,
                    Categorias = categoriasSelectList
                };
                ViewData["Rol"] = HttpContext.Session.GetString("Rol");
                ViewData["IdProducto"] = HttpContext.Session.GetInt32("IdProducto") ?? 0;
                ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
                ViewData["FullName"] = HttpContext.Session.GetString("FullName");
                return View(viewModel);
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

        //GET DATOS USUARIOS
        public IActionResult BuscarProducto()
        {
            ViewData["Rol"] = HttpContext.Session.GetString("Rol");
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BuscarProducto(string nombre)
        {
            // Crear un objeto Producto con el nombre proporcionado
            var model = new Producto
            {
                Nombre = nombre
            };

            // Llamar al método para buscar productos
            var productos = await MetodoBuscar(model);

            // Pasar la lista de productos encontrados a la vista
            return View(productos);
        }

        public async Task<List<Producto>> MetodoBuscar(Producto model)
        {
            // Crear una lista para almacenar los productos encontrados
            List<Producto> productos = new List<Producto>();

            // Ejecutar el procedimiento almacenado para buscar productos por nombre
            string connectionString = _config["ConnectionStrings:cadena"];
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SpBuscarProductosNombre";
                SqlCommand command = new SqlCommand(query, connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Nombre", "%" + model.Nombre + "%");

                await connection.OpenAsync();
                SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // Crear un objeto Producto para cada fila en el resultado y agregarlo a la lista
                    Producto producto = new Producto
                    {
                        IdProducto = Convert.ToInt32(reader["idProducto"]),
                        Nombre = reader["NombreProducto"].ToString(),
                        Precio = Convert.ToDecimal(reader["Precio"]),
                        Stock = Convert.ToInt32(reader["Stock"]),
                        Estado = reader["Estado"].ToString(),
                        IdCategoria = Convert.ToInt32(reader["idCategoria"])
                    };
                    productos.Add(producto);
                }
            }

            // Devolver la lista de productos encontrados
            return productos;
        }


        //desde aca vamos con lo de listado de categorias, asi como el Funcionamiento para editar ok?

        public IActionResult ListarCategorias()
        {

            if (HttpContext.Session.GetString("Rol") == "Administrador" || HttpContext.Session.GetString("Rol") == "Moderador")
            {
                ViewData["Rol"] = HttpContext.Session.GetString("Rol");
                List<CategoriasProductos> categorias = new List<CategoriasProductos>();

                try
                {
                    string connectionString = _config.GetConnectionString("cadena");
                    using (SqlConnection cn = new SqlConnection(connectionString))
                    {
                        cn.Open();

                        SqlCommand command = new SqlCommand("SpListadoCategorias", cn);
                        command.CommandType = CommandType.StoredProcedure;

                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            CategoriasProductos categoria = new CategoriasProductos
                            {
                                IdCategoria = Convert.ToInt32(reader["idCategoria"]),
                                NombreCategoria = reader["NombreCategoria"].ToString(),
                                Descripcion = reader["Descripcion"].ToString()
                            };

                            categorias.Add(categoria);
                        }

                        // Obtener el ID de la categoría de la sesión
                        int idCategoria = HttpContext.Session.GetInt32("IdCategoria") ?? 0;
                        ViewData["IdCategoria"] = idCategoria;
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Ocurrió un error al cargar la lista de categorías.";
                }
                ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
                ViewData["FullName"] = HttpContext.Session.GetString("FullName");
                return View(categorias);
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
        public async Task<IActionResult> ActualizarCategoria(int idCategoria, string nombreCategoria, string descripcion)
        {
            var success = await ActualizarCategoriaDatosAsync(idCategoria, nombreCategoria, descripcion);
            return Json(new { success });
        }

        // Método asincrónico para actualizar categorías en la base de datos
        public async Task<bool> ActualizarCategoriaDatosAsync(int idCategoria, string nombreCategoria, string descripcion)
        {
            try
            {
                string connectionString = _config.GetConnectionString("cadena");
                using (SqlConnection cn = new SqlConnection(connectionString))
                {
                    await cn.OpenAsync(); // Abrir la conexión de manera asíncrona
                    using (SqlCommand command = new SqlCommand("SpActualizarCategorias", cn))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@idCategoria", idCategoria);
                        command.Parameters.AddWithValue("@nombreCategoria", string.IsNullOrEmpty(nombreCategoria) ? (object)DBNull.Value : nombreCategoria);
                        command.Parameters.AddWithValue("@descripcion", string.IsNullOrEmpty(descripcion) ? (object)DBNull.Value : descripcion);

                        int rowsAffected = await command.ExecuteNonQueryAsync(); // Ejecutar la consulta de manera asíncrona

                        if (rowsAffected > 0)
                        {
                            ViewBag.Message = "Categoria actualizada correctamente";
                            return true;
                        }
                        else
                        {
                            ViewBag.Message = "Error al actualizar la categoria";
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejar cualquier excepción aquí
                ViewBag.Message = "Error: " + ex.Message;
                return false;
            }
        }

        public IActionResult RegistrarCategoria()
        {

            if (HttpContext.Session.GetString("Rol") == "Administrador" || HttpContext.Session.GetString("Rol") == "Moderador")
            {
                ViewData["Rol"] = HttpContext.Session.GetString("Rol");
                //RECUPERAR DATOS DE USUARIO OK
                ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
                ViewData["FullName"] = HttpContext.Session.GetString("FullName");
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
        public async Task<IActionResult> RegistrarCategoria(ProductosYCategorias model)
        {


            try
            {
                // Llamada al stored procedure para registrar la nueva categoría
                string connectionString = _config.GetConnectionString("cadena");
                int idCategoria = 0; // Variable para almacenar el ID de la nueva categoría

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand("SPRegistrarCategoria", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@NombreCategoria", model.NombreCategoria); // Corregir aquí
                        command.Parameters.AddWithValue("@Descripcion", model.Descripcion);

                        // Ejecutar el comando y obtener el ID de la nueva categoría
                        var result = await command.ExecuteScalarAsync();
                        if (result != null && int.TryParse(result.ToString(), out idCategoria))
                        {
                            ViewBag.Message = "Categoría registrada correctamente con el ID: " + idCategoria;
                        }
                        else
                        {
                            ViewBag.Message = "Error al registrar la categoría.";
                        }
                    }
                }

                return View(); // Redirigir a la vista correspondiente
            }
            catch (Exception ex)
            {
                ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
                ViewData["FullName"] = HttpContext.Session.GetString("FullName");
                //MANEJO EN CASO DE ERRORES
                ViewBag.Message = "Error: " + ex.Message;
                return View(); // Redirigir a la vista correspondiente
            }
        }
    }
}