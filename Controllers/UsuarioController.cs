using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Session;
using Microsoft.Data.SqlClient;
using System.Data;
using Ecommerce_2024_1_NJD.Models;
using System.Security.Policy;
using NuGet.Protocol.Plugins;
using Ecommerce_2024_1_NJD; // Asegúrate de importar el espacio de nombres donde se encuentra EncryptPass
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Ecommerce_2024_1_NJD.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Ecommerce_2024_1_NJD.Controllers
{
    public class UsuarioController : Controller
    {
        // Instanciar conexión SQL
        public readonly IConfiguration _config;
        private readonly IEmailSender _emailSender;

        public UsuarioController(IConfiguration config, IEmailSender emailSender)
        {
            _config = config;
            _emailSender = emailSender;
        }

        // Método para redirección a página de inicio de sesión
        public IActionResult LoginPage()
        {
            //ELIMINAR DATOS DE USUARIO RECUPERANDO CONTRASEÑA
            HttpContext.Session.Remove("ResultadoBusquedaUsuario");
            HttpContext.Session.Remove("ID");
            HttpContext.Session.Remove("Email");
            HttpContext.Session.Remove("Nombres");
            HttpContext.Session.Remove("Apellidos");
            HttpContext.Session.Remove("CODIGOAUTOGENERADO");
            
            // ELIMINAR DATOS DE REGISTRO
            HttpContext.Session.Remove("REG-CODVERIFICACION");
            HttpContext.Session.Remove("REG-USERNAME");
            HttpContext.Session.Remove("REG-PASSWORD");
            HttpContext.Session.Remove("REG-NOMBRES");
            HttpContext.Session.Remove("REG-APELLIDOS");
            HttpContext.Session.Remove("REG-EMAIL");
            HttpContext.Session.Remove("REG-ROL");

            return View();
        }

        [HttpPost]
        public IActionResult LoginPage(Usuario u)
        {
            try
            {
                string connectionString = _config.GetConnectionString("cadena");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("SP_LOGINV2", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@UserNameOrEmail", u.username);

                    // Encriptar la contraseña proporcionada por el usuario
                    string encryptedPassword = EncryptPass.GetSHA256(u.pass);
                    command.Parameters.AddWithValue("@Password", encryptedPassword);

                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            HttpContext.Session.SetString("IdUsuario", reader["id_usuario"].ToString());
                            HttpContext.Session.SetString("FullName", reader["FullName"].ToString());
                            HttpContext.Session.SetString("Rol", reader["Rol"].ToString());
                            string rol = reader["Rol"].ToString();
                            if (rol == "Administrador" || rol == "Moderador")
                            {
                                // Redirigir al Index del controlador Home si el usuario es un administrador
                                return RedirectToAction("Index", "Dashboard");
                            }

                            if (rol == "Solicitante")
                            {
                                // Para solicitante
                                return RedirectToAction("AprobacionPendiente", "Usuario");
                            }
                        }

                        // Si el usuario no es administrador, redirigir al Portal del ECommerce
                        return RedirectToAction("Portal", "ECommerce");
                    }
                    else
                    {
                        //Excepcion para pasar el fullname como nulo y no mande al controlador error
                        ViewBag.ErrorMessage = "BIENVENIDO";
                        return View("LoginPage", u);
                    }
                }
            }
            catch (Exception ex)
            {
                //CATCH - NECESARIO PARA MANEJAR EL TEMA DEL fullname no sea = null
                ViewBag.ErrorMessage = "Usuario o contraseña incorrectos. Inténtelo nuevamente.";
                return View("LoginPage", u); //carga de nuevo la pagina del LoginPage
            }
        }




        //PRUEBA PARA RECUPERAR ID USUARIO Y FULLNAME = NOMBRES USUARIO, MEDIANTE VIEWDATE
        //DONDE SE INYECTA EL IdUsuario y el Fullname y se pasa como párametros globales a nivel proyecto
        public IActionResult DaleAlRayi()
        {
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            return View();
        }

        // Método para redirección a página de registro
        public IActionResult RegisterPage()
        {

            // ELIMINAR DATOS DE REGISTRO
            HttpContext.Session.Remove("REG-CODVERIFICACION");
            HttpContext.Session.Remove("REG-USERNAME");
            HttpContext.Session.Remove("REG-PASSWORD");
            HttpContext.Session.Remove("REG-NOMBRES");
            HttpContext.Session.Remove("REG-APELLIDOS");
            HttpContext.Session.Remove("REG-EMAIL");
            HttpContext.Session.Remove("REG-ROL");

            //PRUEBA 1
            return View();
        }
        [HttpPost]
        public IActionResult RegisterPage(Usuario reg)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Encriptar la contraseña antes de almacenarla en la base de datos
                    string encryptedPassword = EncryptPass.GetSHA256(reg.pass);

                    string connectionString = _config.GetConnectionString("cadena");

                    using (SqlConnection cn = new SqlConnection(connectionString))
                    {
                        cn.Open();

                        // Verificar si el nombre de usuario ya está en uso
                        string query = "SELECT COUNT(*) FROM Usuarios WHERE username = @username";
                        using (SqlCommand checkUsernameCmd = new SqlCommand(query, cn))
                        {
                            checkUsernameCmd.Parameters.AddWithValue("@username", reg.username);
                            int existingUsernamesCount = (int)checkUsernameCmd.ExecuteScalar();
                            if (existingUsernamesCount > 0)
                            {
                                ViewBag.ErrorEx = "El nombre de usuario ya está en uso.";
                                return View(reg);
                            }
                        }

                        // Verificar si el correo electrónico ya está en uso
                        query = "SELECT COUNT(*) FROM Usuarios WHERE email = @email";
                        using (SqlCommand checkEmailCmd = new SqlCommand(query, cn))
                        {
                            checkEmailCmd.Parameters.AddWithValue("@email", reg.email);
                            int existingEmailsCount = (int)checkEmailCmd.ExecuteScalar();
                            if (existingEmailsCount > 0)
                            {
                                ViewBag.ErrorEx = "El correo electrónico ya está en uso.";
                                return View(reg);
                            }
                        }


                        // CODIGO VERIFICACION REGISTRO
                        string codigo = GenerarCodigo();

                        HttpContext.Session.SetString("REG-CODVERIFICACION", codigo);
                        HttpContext.Session.SetString("REG-USERNAME", reg.username);
                        HttpContext.Session.SetString("REG-PASSWORD", encryptedPassword);
                        HttpContext.Session.SetString("REG-NOMBRES", reg.nombres);
                        HttpContext.Session.SetString("REG-APELLIDOS", reg.apellidos);
                        HttpContext.Session.SetString("REG-EMAIL", reg.email);
                        HttpContext.Session.SetString("REG-FORM", "Clientes");

                        // Envía el correo electrónico después de que el usuario se haya registrado correctamente
                        var receptor = reg.email;
                        var asunto = "Bienvenido a nuestra plataforma";
                        var msg = @"
                            <html style=""background-color: #e9e9e9;"">
                                <style>
                                    .div-cont{background-color: #fff;}
                                    *{padding: 0; margin: 0; font-family: 'Trebuchet MS', sans-serif; }
                                    .img{
                                        background-color: #000;
                                    }
                                </style>

                                <div style=""background-color: #e9e9e9;"">

                                    <div style=""background: #000; text-align: center; padding: 10px 0;"">
                                        <img class=""img"" style=""width: 40%; max-width: 250px;"" src=""https://i.ibb.co/8gvZgm0/purofutbollogoclaro.png"" alt=""PuroFutbol LOGO"">
                                    </div>

                                    <div style=""background-color: rgb(255, 255, 255);width: 90%; max-width: 800px; margin: 15px auto; border-radius: 5px; box-shadow: rgba(0, 0, 0, 0.24) 0px 3px 8px;"">

                                        <div>
                                            <h2 style=""padding: 5px 0; text-align:center"">Bienvenido</h2>
                                            <hr>
                                            <div style=""padding: 5px 0; padding: 20px;"">

                                                <p>Hola, <span style=""font-weight: bold;"">" + reg.nombres + " " + reg.apellidos + @"</span>.</p>
                                                <br>
                                                <p>Este es el código de verificación para continuar con tu registro: <span style=""font-weight: bold;"">" + codigo + @"</span></p>
                                            </div>

                                        </div>
                                    </div><br>

                                </div>
                            </html>
                        ";

                        var mensaje = "<html><body><h1>Bienvenido</h1><p>Hola, " + reg.nombres + " " + reg.apellidos + ".</p><br><p>Este es el codigo de verificacion para continuar con tu registro: " + codigo + "</p></body></html>";

                        _emailSender.SendEmailAsync(receptor, asunto, msg, isHtml: true);

                    }

                    // REDIGE CUANDO EL USUARIO (CLIENTE) SE REGISTRA
                    return RedirectToAction("VerificarCorreo");
                }
                return View(reg);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorEx = "Se produjo un error al procesar tu solicitud. Por favor, inténtalo de nuevo más tarde.";
                return View(reg);
            }
        }



        public IActionResult VerificarCorreo()
        {
            var codverificacion = HttpContext.Session.GetString("REG-CODVERIFICACION");
            var password = HttpContext.Session.GetString("REG-PASSWORD");
            var nombres = HttpContext.Session.GetString("REG-NOMBRES");
            var apellidos = HttpContext.Session.GetString("REG-APELLIDOS");
            var email = HttpContext.Session.GetString("REG-EMAIL");
            var rol = HttpContext.Session.GetString("REG-FORM");

            ViewBag.nombres = nombres;
            ViewBag.apellidos = apellidos;
            ViewBag.email = email;
            ViewBag.password = password;
            ViewBag.rol = rol;

            if (codverificacion != null)
            {
                return View();
            }

            return RedirectToAction("LoginPage");
        }

        [HttpPost]
        public IActionResult ReenviarCodRegistro()
        {
            string NuevoCodigo = GenerarCodigo();
            var rol = HttpContext.Session.GetString("REG-FORM");
            var nombres = HttpContext.Session.GetString("REG-NOMBRES");
            var apellidos = HttpContext.Session.GetString("REG-APELLIDOS");
            var email = HttpContext.Session.GetString("REG-EMAIL");

            HttpContext.Session.SetString("REG-CODVERIFICACION", NuevoCodigo);

            ViewBag.email = email;
            
            // RECEPTOR = EMAIL
            var asunto = "Bienvenido a nuestra plataforma";
            string mensaje;

            if (rol == "Clientes")
            {
                mensaje = "<html><body><h1>Bienvenido</h1><p>Hola, " + nombres + " " + apellidos + ".</p><br><p>Este es el codigo de verificacion para continuar con tu registro: " + NuevoCodigo + "</p></body></html>";
            }
            else
            {
                mensaje = "<html><body><h1>Bienvenido</h1><p>Hola, " + nombres + " " + apellidos + ".</p><br><p>Este es el codigo de verificacion para continuar con tu solicitud y registro: " + NuevoCodigo + "</p></body></html>";
            }
            

            _emailSender.SendEmailAsync(email, asunto, mensaje, isHtml: true);


            return RedirectToAction("VerificarCorreo");
        }


        [HttpPost]
        public IActionResult VerificarCorreo(string codigoIngresado)
        {

            var codverificacion = HttpContext.Session.GetString("REG-CODVERIFICACION");
            var username = HttpContext.Session.GetString("REG-USERNAME");
            var password = HttpContext.Session.GetString("REG-PASSWORD");
            var nombres = HttpContext.Session.GetString("REG-NOMBRES");
            var apellidos = HttpContext.Session.GetString("REG-APELLIDOS");
            var email = HttpContext.Session.GetString("REG-EMAIL");
            var rol = HttpContext.Session.GetString("REG-FORM");

            ViewBag.email = email;

            if (codigoIngresado == codverificacion)
            {
                try
                {
                    string connectionString = _config.GetConnectionString("cadena");

                    using (SqlConnection cn = new SqlConnection(connectionString))
                    {
                        cn.Open();

                        if (rol == "Clientes")
                        {
                            using (SqlCommand command = new SqlCommand("Sp_RegistrarUsuario", cn))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@username", username);
                                command.Parameters.AddWithValue("@pass", password);
                                command.Parameters.AddWithValue("@nombres", nombres);
                                command.Parameters.AddWithValue("@apellidos", apellidos);
                                command.Parameters.AddWithValue("@email", email);

                                command.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // SQL REGISTRAR SOLICITANTE A ADMIN
                            using (SqlCommand command = new SqlCommand("SP_RegistroYSolicitudAdmin", cn))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@username", username);
                                command.Parameters.AddWithValue("@pass", password);
                                command.Parameters.AddWithValue("@nombres", nombres);
                                command.Parameters.AddWithValue("@apellidos", apellidos);
                                command.Parameters.AddWithValue("@email", email);

                                command.ExecuteNonQuery();
                            }
                        }


                        cn.Close();

                        // ELIMINAR DATOS DE REGISTRO
                        HttpContext.Session.Remove("REG-CODVERIFICACION");
                        HttpContext.Session.Remove("REG-USERNAME");
                        HttpContext.Session.Remove("REG-PASSWORD");
                        HttpContext.Session.Remove("REG-NOMBRES");
                        HttpContext.Session.Remove("REG-APELLIDOS");
                        HttpContext.Session.Remove("REG-EMAIL");
                        HttpContext.Session.Remove("REG-FORM");
                        
                        return RedirectToAction("LoginPage");
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "No se pudo registrar usuario: " + ex;
                    return View();
                }
            }

            ViewBag.Error = "El código ingresado no coincide, inténtelo nuevamente.";
            return View();
        }





        // Método para redirección a página de registro
        public IActionResult RegistroAdmin()
        {
            // ELIMINAR DATOS DE REGISTRO
            HttpContext.Session.Remove("REG-CODVERIFICACION");
            HttpContext.Session.Remove("REG-USERNAME");
            HttpContext.Session.Remove("REG-PASSWORD");
            HttpContext.Session.Remove("REG-NOMBRES");
            HttpContext.Session.Remove("REG-APELLIDOS");
            HttpContext.Session.Remove("REG-EMAIL");
            HttpContext.Session.Remove("REG-FORM");

            return View();
        }

        [HttpPost]
        public IActionResult RegistroAdmin(Usuario reg)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Encriptar la contraseña antes de almacenarla en la base de datos
                    string encryptedPassword = EncryptPass.GetSHA256(reg.pass);

                    string connectionString = _config.GetConnectionString("cadena");

                    using (SqlConnection cn = new SqlConnection(connectionString))
                    {
                        cn.Open();

                        // Verificar si el nombre de usuario ya está en uso
                        string query = "SELECT COUNT(*) FROM Usuarios WHERE username = @username";

                        using (SqlCommand checkUsernameCmd = new SqlCommand(query, cn))
                        {
                            checkUsernameCmd.Parameters.AddWithValue("@username", reg.username);
                            int existingUsernamesCount = (int)checkUsernameCmd.ExecuteScalar();
                            if (existingUsernamesCount > 0)
                            {
                                ViewBag.ErrorEx = "El nombre de usuario ya está en uso.";
                                return View(reg);
                            }
                        }

                        // Verificar si el correo electrónico ya está en uso
                        query = "SELECT COUNT(*) FROM Usuarios WHERE email = @email";
                        using (SqlCommand checkEmailCmd = new SqlCommand(query, cn))
                        {
                            checkEmailCmd.Parameters.AddWithValue("@email", reg.email);
                            int existingEmailsCount = (int)checkEmailCmd.ExecuteScalar();
                            if (existingEmailsCount > 0)
                            {
                                ViewBag.ErrorEx = "El correo electrónico ya está en uso.";
                                return View(reg);
                            }
                        }


                        // CODIGO VERIFICACION REGISTRO
                        string codigo = GenerarCodigo();

                        HttpContext.Session.SetString("REG-CODVERIFICACION", codigo);
                        HttpContext.Session.SetString("REG-USERNAME", reg.username);
                        HttpContext.Session.SetString("REG-PASSWORD", encryptedPassword);
                        HttpContext.Session.SetString("REG-NOMBRES", reg.nombres);
                        HttpContext.Session.SetString("REG-APELLIDOS", reg.apellidos);
                        HttpContext.Session.SetString("REG-EMAIL", reg.email);
                        HttpContext.Session.SetString("REG-FORM", "Administradores");

                        // Envía el correo electrónico después de que el usuario se haya registrado correctamente
                        var receptor = reg.email;
                        var asunto = "Bienvenido a nuestra plataforma";

                        var msg = @"
                            <html style=""background-color: #e9e9e9;"">
                                <style>
                                    .div-cont{background-color: #fff;}
                                    *{padding: 0; margin: 0; font-family: 'Trebuchet MS', sans-serif; }
                                    .img{
                                        background-color: #000;
                                    }
                                </style>

                                <div style=""background-color: #e9e9e9;"">

                                    <div style=""background: #000; text-align: center; padding: 10px 0;"">
                                        <img class=""img"" style=""width: 40%; max-width: 250px;"" src=""https://i.ibb.co/8gvZgm0/purofutbollogoclaro.png"" alt=""PuroFutbol LOGO"">
                                    </div>

                                    <div style=""background-color: rgb(255, 255, 255);width: 90%; max-width: 800px; margin: 15px auto; border-radius: 5px; box-shadow: rgba(0, 0, 0, 0.24) 0px 3px 8px;"">

                                        <div>
                                            <h2 style=""padding: 5px 0; text-align:center"">Bienvenido</h2>
                                            <hr>
                                            <div style=""padding: 5px 0; padding: 20px;"">

                                                <p>Hola, <span style=""font-weight: bold;"">" + reg.nombres + " " + reg.apellidos + @"</span>.</p>
                                                <br>
                                                <p>Este es el código de verificación para continuar con tu registro y solicitud de permisos: <span style=""font-weight: bold;"">" + codigo + @"</span></p>
                                            </div>

                                        </div>
                                    </div><br>

                                </div>
                            </html>
                        ";


                        _emailSender.SendEmailAsync(receptor, asunto, msg, isHtml: true);



                        cn.Close();
                    }
                    // REDIGE CUANDO EL USUARIO (CLIENTE) SE REGISTRA
                    return RedirectToAction("VerificarCorreo");
                }
                return View(reg);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorEx = "Se produjo un error al procesar tu solicitud. Por favor, inténtalo de nuevo más tarde. Error: " + ex;
                return View(reg);
            }
        }




        public List<Usuario> ListadoSolicitantes()
        {

            List<Usuario> solicitantes = new List<Usuario>();

            try
            {
                string connectionString = _config.GetConnectionString("cadena");
                using (SqlConnection cn = new SqlConnection(connectionString))
                {
                    cn.Open();

                    SqlCommand command = new SqlCommand("SP_ListarSolicitantes", cn);
                    command.CommandType = CommandType.StoredProcedure;

                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Usuario solicitante = new Usuario
                        {
                            idSolicitud = Convert.ToInt32(reader["IdSolicitud"]),
                            nombres = reader["Nombres"].ToString(),
                            email = reader["email"].ToString(),
                            FechaRegSolicitud = Convert.ToDateTime(reader["FechaRegSolicitud"]),
                            FechaAprobacion = reader["FechaAprobacion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FechaAprobacion"]),
                            EstadoSolicitud = reader["EstadoSolicitud"].ToString(),
                            id_usuario = Convert.ToInt32(reader["id_usuario"])
                        };

                        solicitantes.Add(solicitante);
                    }

                }
            }
            catch (Exception ex)
            {
                // Err
                ViewBag.ErrorMessage = "Ocurrió un error al cargar la lista de solicitantes." + ex;
            }

            return solicitantes;
        }

        public IActionResult ListarSolicitantes(string? errorMessage)
        {

            if (HttpContext.Session.GetString("Rol") == "Administrador")
            {
                ViewData["Rol"] = HttpContext.Session.GetString("Rol");
                ViewBag.ErrorMessage = errorMessage;
                ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
                ViewData["FullName"] = HttpContext.Session.GetString("FullName");

                List<Usuario> solicitantes = ListadoSolicitantes();

                ViewBag.Solicitantes = solicitantes;

                return View();
            }

            else if (HttpContext.Session.GetString("Rol") == "Moderador")
            {
                return RedirectToAction("Index", "Dashboard");
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
        public IActionResult AgregarAdministrador(int idUsuario, string password, int idSolicitante)
        {

            if (password.IsNullOrEmpty())
            {
                password = "-_-";
            }




            string idUsuarioString = HttpContext.Session.GetString("IdUsuario");
            idUsuario = Convert.ToInt32(idUsuarioString);

            string encryptedPassword = EncryptPass.GetSHA256(password);
            
            try
            {
                string connectionString = _config.GetConnectionString("cadena");
                using (SqlConnection cn = new SqlConnection(connectionString))
                {
                    cn.Open();

                    SqlCommand cmd = new SqlCommand("SP_AGREGARADMINISTRADOR", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmd.Parameters.AddWithValue("@Password", encryptedPassword);
                    cmd.Parameters.AddWithValue("@IdSolicitante", idSolicitante);

                    cmd.ExecuteNonQuery();

                    HttpContext.Session.SetString("RememberMe", "true");

                    var userMail = ObtenerEmailUsuario(idSolicitante);
                    var asunto = "Solicitud aprobada";
                    var nombreUsuario = ObtenerNombresUsuario(idSolicitante);
                    var msg = @"
                            <html style=""background-color: #e9e9e9;"">
                                <style>
                                    .div-cont{background-color: #fff;}
                                    *{padding: 0; margin: 0; font-family: 'Trebuchet MS', sans-serif; }
                                    .img{
                                        background-color: #000;
                                    }
                                </style>

                                <div style=""background-color: #e9e9e9;"">

                                    <div style=""background: #000; text-align: center; padding: 10px 0;"">
                                        <img class=""img"" style=""width: 40%; max-width: 250px;"" src=""https://i.ibb.co/8gvZgm0/purofutbollogoclaro.png"" alt=""PuroFutbol LOGO"">
                                    </div>

                                    <div style=""background-color: rgb(255, 255, 255);width: 90%; max-width: 800px; margin: 15px auto; border-radius: 5px; box-shadow: rgba(0, 0, 0, 0.24) 0px 3px 8px;"">

                                        <div>
                                            <h2 style=""padding: 5px 0; text-align:center; margin:0"">Solicitud aprobada</h2>
                                            <hr>
                                            <div style=""padding: 5px 0; padding: 20px;"">

                                                <p>Hola, <span style=""font-weight: bold;"">" + nombreUsuario + @"</span>.</p>
                                                <br>
                                                <p>Su solicitud para obtener permisos de moderador ha sido aceptada, bienvenido a PuroFutbol.</p>
                                            </div>

                                        </div>
                                    </div><br>

                                </div>
                            </html>
                        ";


                    _emailSender.SendEmailAsync(userMail, asunto, msg, isHtml: true);

                    return RedirectToAction("ListarSolicitantes");
                }
            }
            catch (Exception ex)
            {
                HttpContext.Session.SetString("RememberMe", "false");
                return RedirectToAction("ListarSolicitantes", new { errorMessage = ex.Message });
            }

        }

        private string ObtenerEmailUsuario(int idUsuario)
        {
            string connectionString = _config.GetConnectionString("cadena");
            string userEmail = "";

            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();

                SqlCommand emailCmd = new SqlCommand("SELECT TOP 1 Email FROM Usuarios WHERE id_usuario = @IdUsuario", cn);
                emailCmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                using (SqlDataReader reader = emailCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        userEmail = reader["Email"].ToString();
                    }
                }
            }

            return userEmail;
        }


        private string ObtenerNombresUsuario(int idUsuario)
        {
            string connectionString = _config.GetConnectionString("cadena");
            string nombres = "";

            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();

                SqlCommand emailCmd = new SqlCommand("SELECT TOP 1 nombres, apellidos FROM Usuarios WHERE id_usuario = @IdUsuario", cn);
                emailCmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                using (SqlDataReader reader = emailCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        nombres = reader["nombres"].ToString() + " " + reader["apellidos"].ToString();
                    }
                }
            }

            return nombres;
        }




        public IActionResult HistorialSolicitudes()
        {

            if (HttpContext.Session.GetString("Rol") == "Administrador")
            {
                List<Usuario> solicitantes = ListadoSolicitantes();

                ViewBag.Solicitantes = solicitantes;
                ViewData["Rol"] = HttpContext.Session.GetString("Rol");
                ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
                ViewData["FullName"] = HttpContext.Session.GetString("FullName");

                return View();
            }
            else if (HttpContext.Session.GetString("Rol") == "Moderador")
            {
                return RedirectToAction("Index", "Dashboard");
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


        //RECUPERAR CONTRASEÑA
        public IActionResult RecuperaTuContraseña()
        {
            if (HttpContext.Session.GetString("ResultadoBusquedaUsuario") == "Exito 1")
            {
                return RedirectToAction("RecuperaTuContraseña2");
            }

            if (HttpContext.Session.GetString("ResultadoBusquedaUsuario") == "Exito 2")
            {
                return RedirectToAction("CambiarContraseña");
            }

            //PA REGRESAR mensaje
            ViewBag.ResultadoBusqueda = ".";


            return View();
        }

        [HttpPost]
        public IActionResult RecuperaTuContraseña(string Email)
        {
            string connectionString = _config.GetConnectionString("cadena");
            var usuario = new Usuario(); 
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("RecuperarContraseña", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Email", Email);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {

                            var nombres = reader["Nombres"].ToString();
                            var apellidos = reader["Apellidos"].ToString();
                            var idUsuario = reader["id_usuario"].ToString();

                            string codigo = GenerarCodigo();

                            //Mandar correo con codigo (pues la verificacion anterior devuelve al usuario)
                            var receptor = Email;
                            var asunto = "Código de recuperacion";

                            var msg = @"
                                        <html style=""background-color: #e9e9e9;"">
                                            <style>
                                                .div-cont{background-color: #fff;}
                                                *{padding: 0; margin: 0; font-family: 'Trebuchet MS', sans-serif; }
                                                .img{
                                                    background-color: #000;
                                                }
                                            </style>

                                            <div style=""background-color: #e9e9e9;"">

                                                <div style=""background: #000; text-align: center; padding: 10px 0;"">
                                                    <img class=""img"" style=""width: 40%; max-width: 250px;"" src=""https://i.ibb.co/8gvZgm0/purofutbollogoclaro.png"" alt=""PuroFutbol LOGO"">
                                                </div>

                                                <div style=""background-color: rgb(255, 255, 255);width: 90%; max-width: 800px; margin: 15px auto; border-radius: 5px; box-shadow: rgba(0, 0, 0, 0.24) 0px 3px 8px;"">

                                                    <div>
                                                        <h2 style=""padding: 5px 0; text-align:center; margin:0"">Código de recuperación</h2>
                                                        <hr>
                                                        <div style=""padding: 5px 0; padding: 20px;"">

                                                            <p>Hola, <span style=""font-weight: bold;"">" + nombres + " " + apellidos + @"</span>.</p>
                                                            <br>
                                                            <p>Este es el código de recuperación para tu cambio de contraseña: <span style=""font-weight: bold;"">" + codigo + @"</span></p>
                                                        </div>

                                                    </div>
                                                </div><br>

                                            </div>
                                        </html>
                                    ";

                            var mensaje = "<html><body><h1>Hola " + nombres + " " + apellidos + " </h1><p>Aquí tu código de recuperación: " + codigo +"</p></body></html>";

                            _emailSender.SendEmailAsync(receptor, asunto, msg, isHtml: true);



                            HttpContext.Session.SetString("ResultadoBusquedaUsuario", "Exito 1");
                            HttpContext.Session.SetString("ID", idUsuario);
                            HttpContext.Session.SetString("Email", Email);
                            HttpContext.Session.SetString("Nombres", nombres);
                            HttpContext.Session.SetString("Apellidos", apellidos);
                            HttpContext.Session.SetString("CODIGOAUTOGENERADO", codigo);

                            return RedirectToAction("RecuperaTuContraseña2");
                        }
                        else
                        {
                            // Si no existe usuario asociado al correo, redirige con error
                            ViewBag.ResultadoBusqueda = "El correo electrónico proporcionado no está registrado en nuestra base de datos.";
                            return View();
                        }
                    }
                }
            }
        }

        private string GenerarCodigo()
        {

            Random random = new Random();

            // Genera un numero de 6 digitos
            int codigo = random.Next(100000, 999999);

            // Convertir codigo a string
            return codigo.ToString();
        }


        public IActionResult RecuperaTuContraseña2(string ResultadoBusquedaUsuario)
        {

            if (HttpContext.Session.GetString("ResultadoBusquedaUsuario") == "Exito 1")
            {
                // Obtener datos de sesion

                ViewBag.ID = HttpContext.Session.GetString("ID");
                ViewBag.Email = HttpContext.Session.GetString("Email");
                ViewBag.Nombres = HttpContext.Session.GetString("Nombres");
                ViewBag.Apellidos = HttpContext.Session.GetString("Apellidos");
                ViewBag.CODIGOAUTOGENERADO = HttpContext.Session.GetString("CODIGOAUTOGENERADO");



                return View();
            }

            return RedirectToAction("RecuperaTuContraseña");
        }

        [HttpPost]
        public IActionResult RecuperaTuContraseña2(int codigoIngresado)
        {
            ViewBag.ID = HttpContext.Session.GetString("ID");
            ViewBag.Email = HttpContext.Session.GetString("Email");
            ViewBag.Nombres = HttpContext.Session.GetString("Nombres");
            ViewBag.Apellidos = HttpContext.Session.GetString("Apellidos");
            ViewBag.CODIGOAUTOGENERADO = HttpContext.Session.GetString("CODIGOAUTOGENERADO");


            if (codigoIngresado.ToString() == HttpContext.Session.GetString("CODIGOAUTOGENERADO"))
            {
                HttpContext.Session.SetString("ResultadoBusquedaUsuario", "Exito 2");
                return RedirectToAction("CambiarContraseña");
            }

            ViewBag.ResultadoVerificacion = "Vuelva a ingresar el código";
            return View();
        }

        public IActionResult CambiarContraseña()
        {
            ViewBag.ResultadoVerificacion = HttpContext.Session.GetString("ResultadoBusquedaUsuario");

            if (HttpContext.Session.GetString("ResultadoBusquedaUsuario") == "Exito 2")
            {
                return View();
            }

            if (HttpContext.Session.GetString("ResultadoBusquedaUsuario") == "Exito 1")
            {

                return RedirectToAction("RecuperaTuContraseña2");
            }
            
            return RedirectToAction("RecuperaTuContraseña");
        }

        [HttpPost]
        public IActionResult CambiarContraseña(string pw, string pwconfirm)
        {
            
            

            // Comparar ambos campos
            if ( pw == pwconfirm )
            {
                string encryptedpw = EncryptPass.GetSHA256(pw);

                string nuevapw = encryptedpw.Substring(0, 20);
                string? email = HttpContext.Session.GetString("Email");
                string? usuarioid = HttpContext.Session.GetString("ID");


                try{
                    string connectionString = _config.GetConnectionString("cadena");
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        using (var command = new SqlCommand("CambiarContraseña", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@UsuarioID", usuarioid);
                            command.Parameters.AddWithValue("@Email", email);
                            command.Parameters.AddWithValue("@NuevaPW", nuevapw);

                            command.ExecuteNonQuery();
                        }

                        //Con esto mueren los datos de sesion
                        HttpContext.Session.Remove("ResultadoBusquedaUsuario");
                        HttpContext.Session.Remove("ID");
                        HttpContext.Session.Remove("Email");
                        HttpContext.Session.Remove("Nombres");
                        HttpContext.Session.Remove("Apellidos");
                        HttpContext.Session.Remove("CODIGOAUTOGENERADO");

                        return RedirectToAction("LoginPage");
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ResultadoComparacion = "Error: " + ex;
                    return View();
                }
            }

            ViewBag.ResultadoComparacion = "Las contraseñas no coinciden.";
            return View();

        }

        public IActionResult ReiniciarProcesoRecuperacionContraseña()
        {
            HttpContext.Session.Remove("ResultadoBusquedaUsuario");
            HttpContext.Session.Remove("ID");
            HttpContext.Session.Remove("Email");
            HttpContext.Session.Remove("Nombres");
            HttpContext.Session.Remove("Apellidos");
            HttpContext.Session.Remove("CODIGOAUTOGENERADO");
           
            return RedirectToAction("RecuperaTuContraseña");
        }


        //SI ERES SOLICITANTE
        public IActionResult AprobacionPendiente()
        {
            return View();
        }




        //PARA CERRAR SESSION
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); //CLEAR A TODOS LOS MOVIEMIENTOS DE UNA SESION
            return RedirectToAction("LoginPage");
        }
    }
}