using System.ComponentModel.DataAnnotations;

namespace Ecommerce_2024_1_NJD.Models
{
    public class Pago
    {
        public int idPago { get; set; }
        public int idPedido { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime Fecha { get; set; }
        public string? MetodoPago { get; set; }
    }
}
