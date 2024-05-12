using System.ComponentModel.DataAnnotations;
namespace Ecommerce_2024_1_NJD.Models
{
    public class Usuario
    {
        public int id_usuario { get; set; }
        [Display(Name="Usuario"), Required]public string username { get; set; }
        [Display(Name ="Password"), Required]public string pass { get; set; }
        public string nombres { get; set; }
        public string apellidos { get; set; }
        public string Rol { get; set; }
        public string email { get; set; }
        
        // PARA SOLICITANTES
        public int idSolicitud { get; set; }
        public DateTime FechaRegSolicitud { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public string? EstadoSolicitud { get; set; }


        public Usuario()
        {
            id_usuario = 0;
            username = "";
            pass = "";
            nombres = "";
            apellidos = "";
            Rol = "";
            email = "";
        }
    }
}
 