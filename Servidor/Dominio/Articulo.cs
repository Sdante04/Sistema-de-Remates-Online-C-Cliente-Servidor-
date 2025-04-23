using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
