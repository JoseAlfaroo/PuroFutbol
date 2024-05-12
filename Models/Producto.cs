using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce_2024_1_NJD.Models
{
    public class Producto
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; }
        public IFormFile Imagen { get; set; } // Propiedad para la imagen
        public string NombreCategoria { get; set; } // Nombre de la categoría

        public decimal Precio { get; set; }
        public int Stock { get; set; }

        [Column(TypeName = "datetime2")]
        [Display(Name = "Fecha de Registro")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime FechaRegistro { get; set; }
        public string Estado { get; set; }
        public int IdCategoria { get; set; }


        //PARA EL FORMULARIO DE REGISTRO  PEDIDO
        public string dni { get; set; }
        public string direccionDestino { get; set; }

        public int cantidad { get; set; }
        public decimal monto { get { return Precio * cantidad; } }

        //PARA EL DETALLE DEL PEDIDO
        public int IDDetallesPedido { get; set; }
        public string ImagenUrl { get; internal set; }
        public string NombreArchivoImagen { get; set; } // Propiedad para el nombre del archivo de imagen


        // PARA TOP 5 VENDIDOS
        public decimal TotalIngresos { get; set; }
        public int NroVentas { get; set; }


        //PRODUCTO CONSTRUCTOR
        public Producto()
        {
        }
    } 
}
 