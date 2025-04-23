using Servidor.Dominio;
using System.Globalization;

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
            string nombreArchivo = partes.Length > 5 ? partes[5] : null;

            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(descripcion) || string.IsNullOrWhiteSpace(categoria))
                return "Título, descripción y categoría son obligatorios.";

            if (!int.TryParse(precioStr, out int precioBase))
                return "Precio base inválido. Debe ser un número entero.";

            if (!DateTime.TryParseExact(fechaStr, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaCierre))
                return "Fecha de cierre inválida. Usa el formato dd-MM-yyyy HH:mm.";

            Articulo nuevo = new()
            {
                Titulo = titulo,
                Descripcion = descripcion,
                Categoria = categoria,
                PrecioBase = precioBase,
                FechaCierre = fechaCierre,
                ImagenNombreArchivo = nombreArchivo,
                Usuario = usuario
            };

            _articulos.Add(nuevo);
            esExitoso = true;
            return $"El artículo '{titulo}' fue publicado correctamente.";
        }

        public string ValidarDatosArticulo(string datos)
        {
            bool ok;
            PublicarArticulo(datos, "temporal", out ok);
            return ok ? "VALIDO" : "INVALIDO";
        }

    }
}
