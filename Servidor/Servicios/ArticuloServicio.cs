using Servidor.Dominio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor.Servicios
{
    public class ArticuloServicio
    {
        private List<Articulo> _articulos = new();


        public string PublicarArticulo(string datos, string usuario, out bool esExitoso)
        {
            esExitoso = false;
            var partes = datos.Split('|');
            if (partes.Length < 5)
                return "Datos insuficientes para publicar el artículo.";

            string titulo = partes[0];
            string descripcion = partes[1];
            string categoria = partes[2];
            string precioStr = partes[3];
            string fechaStr = partes[4];
            string imagenBase64 = partes.Length > 5 ? partes[5] : null;

            if (!int.TryParse(precioStr, out int precioBase))
                return "Precio base inválido.";

            if (!DateTime.TryParseExact(fechaStr, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaCierre))
                return "Fecha de cierre inválida. Use formato dd-MM-yyyy HH:mm.";

            if (!string.IsNullOrEmpty(imagenBase64) && imagenBase64.Length > 1000000)
                return "La imagen es demasiado grande.";

            Articulo nuevo = new()
            {
                Titulo = titulo,
                Descripcion = descripcion,
                Categoria = categoria,
                PrecioBase = precioBase,
                FechaCierre = fechaCierre,
                ImagenBase64 = imagenBase64,
                Usuario = usuario
            };

            _articulos.Add(nuevo);
            esExitoso = true;
            return $"El artículo '{titulo}' fue publicado correctamente.";
        }
    }
}
