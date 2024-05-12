namespace Ecommerce_2024_1_NJD.Models
{
    public class UsuarioRegPedido
    {
        //PARA EL FORMULARIO DE REGISTRO  PEDIDO
        //idPedido
        public int idPedido { get; set; }
        //CAMPO ESTADO
        public string Estado { get; set; }
        // monto = REPRESENTA AL MONTO TODAL  PRECIO * UNIDAD
        public decimal monto { get { return Precio * cantidad; } }
        //FECHA DEL PEDIDO
        public DateTime FechaPedido { get; set; }
        //id_usuario
        public int idUsuario { get; set; }
        //DNI Persona que recoge el producto si
        public string dni { get; set; }
        //DIRECCION DE DESTINO A QUIEN VA 
        public string direccionDestino { get; set; }
        public decimal Precio { get; set; }
        public int cantidad { get; set; }
        
    }
}
