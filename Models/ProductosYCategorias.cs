using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ecommerce_2024_1_NJD.Models
{
    public class ProductosYCategorias
    {
        public List<Producto> Productos { get; set; }
        public SelectList Categorias { get; set; }
        public int IdCategoria { get; set; }


        //NUEVOS ESPACIOS PARA TRABAJAR
        public string Descripcion { get; set; }
        public string NombreCategoria { get; set; }
    }
}
