using Ecommerce_2024_1_NJD.Interfaces;
using Ecommerce_2024_1_NJD.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Ecommerce_2024_1_NJD.Controllers
{
    public class HomeController : Controller
    {
        //READONLY
        private readonly IEmailSender _emailSender;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IEmailSender emailSender)
        {
            _logger = logger;
            _emailSender = emailSender;
        }
        
        public IActionResult Index()
        {
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");
            return View();
        }

        // EMAIL PRUEBA
        /*
        public async Task<IActionResult> Index()
        {
            ViewData["IdUsuario"] = HttpContext.Session.GetString("IdUsuario");
            ViewData["FullName"] = HttpContext.Session.GetString("FullName");

            var receptor = "already.5.35@gmail.com";
            var asunto = "Probando";
            var mensaje = "<html><body><h1>Hola</h1><p>Este es un correo electrónico con contenido HTML.</p></body></html>";

            await _emailSender.SendEmailAsync(receptor, asunto, mensaje, isHtml: true);
            return View();
        }*/

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}