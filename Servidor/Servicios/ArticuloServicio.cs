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

        public string ObtenerArticulosDeUsuario(string usuario)
        {
            var articulos = _articulos.Where(a => a.Usuario == usuario).ToList();
            if (!articulos.Any()) return "SIN_ARTICULOS";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < articulos.Count; i++)
            {
                var a = articulos[i];
                sb.AppendLine($"{i + 1}. {a.Titulo} | Descripción: {a.Descripcion} | Categoría: {a.Categoria} | Precio: {a.PrecioBase} | Cierra: {a.FechaCierre:dd-MM-yyyy HH:mm}");
            }
            return sb.ToString();
        }

        public string EditarArticulo(string datos, string usuario, out bool exito)
        {
            exito = false;
            var partes = datos.Split('|');
            if (partes.Length < 7)
                return "Datos insuficientes para editar.";

            int index = int.Parse(partes[0]);
            string nuevoTitulo = partes[1];
            string descripcion = partes[2];
            string categoria = partes[3];
            string precioStr = partes[4];
            string fechaStr = partes[5];
            string imagen = partes[6];

            var articulosUsuario = _articulos.Where(a => a.Usuario == usuario).ToList();
            if (index < 1 || index > articulosUsuario.Count)
                return "Índice inválido.";

            var articulo = articulosUsuario[index - 1];

            if (articulo.FechaCierre <= DateTime.Now)
                return "No se puede editar un remate finalizado.";

            if (!string.IsNullOrWhiteSpace(nuevoTitulo))
                articulo.Titulo = nuevoTitulo;
            if (!string.IsNullOrWhiteSpace(descripcion))
                articulo.Descripcion = descripcion;
            if (!string.IsNullOrWhiteSpace(categoria))
                articulo.Categoria = categoria;
            if (!string.IsNullOrWhiteSpace(precioStr) && int.TryParse(precioStr, out int nuevoPrecio))
                articulo.PrecioBase = nuevoPrecio;
            if (!string.IsNullOrWhiteSpace(fechaStr) && DateTime.TryParseExact(fechaStr, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime nuevaFecha))
                articulo.FechaCierre = nuevaFecha;
            if (!string.IsNullOrWhiteSpace(imagen))
                articulo.ImagenNombreArchivo = imagen;

            exito = true;
            return $"Artículo '{articulo.Titulo}' editado correctamente.";
        }
    }
}
