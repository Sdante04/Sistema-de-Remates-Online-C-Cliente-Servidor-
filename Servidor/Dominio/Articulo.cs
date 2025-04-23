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
        public string? ImagenBase64 { get; set; } // Opcional
        public string Usuario { get; set; } // Para asociar al publicador
    }
}
