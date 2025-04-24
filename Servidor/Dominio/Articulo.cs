
namespace Servidor.Dominio
{
    public class Articulo
    {
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; }
        public int PrecioBase { get; set; }
        public DateTime FechaCierre { get; set; }
        public string? ImagenNombreArchivo { get; set; }
        public string Usuario { get; set; }
        public List<Oferta> Ofertas { get; set; } = new();

    }
}