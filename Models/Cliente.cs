namespace Ecommerce_2024_1_NJD.Models
{
    public class Cliente
    {
        public int? Id_usuario { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Email { get; set; }
        public int? CantidadPedidos { get; set; }
        public int? CantidadPagos { get; set; }

    }
}
