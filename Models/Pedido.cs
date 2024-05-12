using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce_2024_1_NJD.Models
{
    public class Pedido
    {

        public int idPedido { get; set; }
        public string estadoPedido { get; set; }
        [Column(TypeName = "datetime2")]
        [Display(Name = "Fecha de Registro")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime fechaRegistroPedido { get; set; }
        public int idUsuario { get; set; }
        public string dniCliente { get; set; }
        public string direccionDestino { get; set; }



        //////DETALE PEDIDO//////

        //public int idPedido { get; set; }
        //PARA RECUPERAR LA IMAGEN , idProducto = imagen
        public int idProducto { get; set; }
        public decimal Precio { get; set; }
        public int cantidad { get; set; }
        //RECUPERA EL MONTO TOTAL DE LOS PEDIDO = $Precio
        public decimal monto { get { return Precio * cantidad; } }


        public string NombreProducto { get; set; }
        public List<Pedido> detallesPedido { get; internal set; }

        //Obtener datos de pago
        public int idPago { get; set; }

        public DateTime Fecha { get; set; }
        public string? MetodoPago { get; set; }


        // PARA LISTADO EN DASHBOARD
        public string? Cliente { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? FechaPago { get; set; }


    }
}
