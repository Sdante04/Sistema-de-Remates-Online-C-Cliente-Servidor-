using Servidor.Dominio;
using System;
using System.Globalization;
using System.Text;

namespace Servidor.Servicios
{
    public class ArticuloServicio
    {
        private readonly List<Articulo> _articulos = new();
        private readonly object _lockArticulos = new object();

        public string PublicarArticulo(string datos, string usuario, out bool esExitoso)
        {
            esExitoso = false;
            var partes = datos.Split('|');
            if (partes.Length < 5)
                return "Datos insuficientes para publicar el artículo";

            string titulo = partes[0];
            string descripcion = partes[1];
            string categoria = partes[2];
            string precioStr = partes[3];
            string fechaStr = partes[4];
            string nombreArchivo = partes.Length > 5 ? partes[5] : null;

            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(descripcion) || string.IsNullOrWhiteSpace(categoria))
                return "Título, descripción y categoría son obligatorios";

            if (!int.TryParse(precioStr, out int precioBase))
                return "Precio base inválido. Debe ser un número entero";

            if (!DateTime.TryParseExact(fechaStr, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaCierre))
                return "Fecha de cierre inválida. Usa el formato dd-MM-yyyy HH:mm";

            var nuevo = new Articulo
            {
                Titulo = titulo,
                Descripcion = descripcion,
                Categoria = categoria,
                PrecioBase = precioBase,
                FechaCierre = fechaCierre,
                ImagenNombreArchivo = nombreArchivo,
                Usuario = usuario
            };

            lock (_lockArticulos)
            {
                _articulos.Add(nuevo);
            }

            esExitoso = true;
            return $"El artículo '{titulo}' fue publicado correctamente";
        }

        public bool ValidarDatosArticulo(string datos)
        {
            var partes = datos.Split('|');
            if (partes.Length < 5) return false;

            string titulo = partes[0];
            string descripcion = partes[1];
            string categoria = partes[2];
            string precioStr = partes[3];
            string fechaStr = partes[4];

            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(descripcion) || string.IsNullOrWhiteSpace(categoria))
                return false;
            if (!int.TryParse(precioStr, out _))
                return false;
            if (!DateTime.TryParseExact(fechaStr, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return false;

            return true;
        }

        public string ObtenerArticulosDeUsuario(string usuario)
        {
            ActualizarEstadoRemates();

            lock (_lockArticulos)
            {
                var articulos = _articulos.Where(a => a.Usuario == usuario).ToList();
                if (!articulos.Any()) return "SIN_ARTICULOS";

                var sb = new StringBuilder();
                for (int i = 0; i < articulos.Count; i++)
                {
                    var a = articulos[i];
                    sb.AppendLine($"{i + 1}. {a.Titulo} | Descripción: {a.Descripcion} | Categoría: {a.Categoria} | Precio: {a.PrecioBase} | Cierra: {a.FechaCierre:dd-MM-yyyy HH:mm}");
                }
                return sb.ToString();
            }
        }

        public string ObtenerTodosLosArticulosEnRemate()
        {
            ActualizarEstadoRemates();

            lock (_lockArticulos)
            {
                var remates = _articulos.Where(a => !a.Finalizado).ToList();
                if (!remates.Any()) return "SIN_ARTICULOS";

                var sb = new StringBuilder();
                for (int i = 0; i < remates.Count; i++)
                {
                    var a = remates[i];
                    sb.AppendLine($"{i + 1}. {a.Titulo} | Descripción: {a.Descripcion} | Categoría: {a.Categoria} | Precio: {a.PrecioBase} | Cierra: {a.FechaCierre:dd-MM-yyyy HH:mm}");
                }
                return sb.ToString();
            }
        }

        public string EditarArticulo(string datos, string usuario, out bool exito)
        {
            exito = false;
            var partes = datos.Split('|');
            if (partes.Length < 7)
                return "Datos insuficientes para editar";

            int index = int.Parse(partes[0]);
            string nuevoTitulo = partes[1];
            string descripcion = partes[2];
            string categoria = partes[3];
            string precioStr = partes[4];
            string fechaStr = partes[5];
            string imagen = partes[6];

            lock (_lockArticulos)
            {
                var articulosUsuario = _articulos.Where(a => a.Usuario == usuario).ToList();
                if (index < 1 || index > articulosUsuario.Count)
                    return "Índice inválido";

                var articulo = articulosUsuario[index - 1];

                if (articulo.FechaCierre <= DateTime.Now)
                    return "No se puede editar un remate finalizado";

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
            }

            exito = true;
            return $"Artículo editado correctamente";
        }

        public string RealizarOferta(string datos, string usuario)
        {
            var partes = datos.Split('|');
            if (partes.Length != 2)
                return "Datos inválidos. Se esperaba: índice|monto";

            if (!int.TryParse(partes[0], out int indice))
                return "Índice inválido";
            if (!int.TryParse(partes[1], out int montoOfertado))
                return "Monto inválido";

            lock (_lockArticulos)
            {
                var articulosEnRemate = _articulos.Where(a => a.FechaCierre > DateTime.Now && !a.Finalizado).ToList();
                if (indice < 1 || indice > articulosEnRemate.Count)
                    return "Índice fuera de rango";

                var articulo = articulosEnRemate[indice - 1];
                int ofertaMaxima = articulo.Ofertas.Any() ? articulo.Ofertas.Max(o => o.Monto) : articulo.PrecioBase;

                if (montoOfertado < articulo.PrecioBase)
                    return "La oferta es menor al precio base";
                if (montoOfertado < ofertaMaxima * 1.1)
                    return $"La oferta debe superar al menos un 10% la oferta máxima actual: {ofertaMaxima}";

                articulo.Ofertas.Add(new Oferta
                {
                    Usuario = usuario,
                    Monto = montoOfertado,
                    Fecha = DateTime.Now
                });

                return $"Oferta registrada: {montoOfertado} por {usuario} para '{articulo.Titulo}'";
            }
        }

        public string ConsultarArticulo(string indiceStr)
        {
            ActualizarEstadoRemates();

            lock (_lockArticulos)
            {
                if (!int.TryParse(indiceStr, out int indice))
                    return "Índice inválido.";

                var remates = _articulos.ToList();
                if (indice < 1 || indice > remates.Count)
                    return "Índice fuera de rango.";

                var a = remates[indice - 1];
                StringBuilder sb = new();

                sb.Append(GenerarDetalleBasico(a));
                sb.AppendLine($"Tiempo restante: {ObtenerTiempoRestante(a.FechaCierre)}");
                sb.AppendLine(GenerarHistorialOfertas(a));

                return sb.ToString();
            }
        }

        private string GenerarDetalleBasico(Articulo a)
        {
            StringBuilder sb = new();
            sb.AppendLine($"Título: {a.Titulo}");
            sb.AppendLine($"Descripción: {a.Descripcion}");
            sb.AppendLine($"Categoría: {a.Categoria}");
            sb.AppendLine($"Precio base: {a.PrecioBase}");
            sb.AppendLine($"Fecha de cierre: {a.FechaCierre:dd-MM-yyyy HH:mm}");

            if (!string.IsNullOrEmpty(a.ImagenNombreArchivo))
                sb.AppendLine($"Imagen asociada: {a.ImagenNombreArchivo}");

            sb.AppendLine($"Estado: {(a.Finalizado ? "Finalizado" : "Activo")}");

            if (a.Finalizado && !string.IsNullOrEmpty(a.UsuarioGanador))
                sb.AppendLine($"Ganador: {a.UsuarioGanador}");

            return sb.ToString();
        }

        private string GenerarHistorialOfertas(Articulo a)
        {
            StringBuilder sb = new();
            sb.AppendLine("Historial de ofertas:");

            if (a.Ofertas.Count == 0)
            {
                sb.AppendLine("  (Sin ofertas aún)");
            }
            else
            {
                foreach (var oferta in a.Ofertas.OrderByDescending(o => o.Fecha))
                {
                    sb.AppendLine($"  - {oferta.Usuario} ofertó {oferta.Monto} el {oferta.Fecha:dd-MM-yyyy HH:mm}");
                }
            }

            return sb.ToString();
        }

        private string ObtenerTiempoRestante(DateTime fechaCierre)
        {
            DateTime ahora = DateTime.Now;

            if (fechaCierre <= ahora)
                return "Remate finalizado.";

            TimeSpan ts = fechaCierre - ahora;

            int totalDias = ts.Days;
            int totalHoras = ts.Hours;
            int totalMinutos = ts.Minutes;

            int años = totalDias / 365;
            int meses = (totalDias % 365) / 30;
            int dias = (totalDias % 365) % 30;

            List<string> partes = new();

            if (años > 0) partes.Add($"{años} año{(años > 1 ? "s" : "")}");
            if (meses > 0) partes.Add($"{meses} mes{(meses > 1 ? "es" : "")}");
            if (dias > 0) partes.Add($"{dias} día{(dias > 1 ? "s" : "")}");

            if (totalDias == 0)
            {
                if (totalHoras > 0)
                    partes.Add($"{totalHoras} hora{(totalHoras > 1 ? "s" : "")}");
                else if (totalMinutos > 0)
                    partes.Add($"{totalMinutos} minuto{(totalMinutos > 1 ? "s" : "")}");
                else
                    partes.Add("¡A punto de expirar!");
            }

            return string.Join(", ", partes);
        }



        public string EliminarArticulo(string datos, string usuario, out bool eliminado)
        {
            eliminado = false;

            if (!int.TryParse(datos, out int indice))
                return "Índice inválido";

            lock (_lockArticulos)
            {
                var articulosUsuario = _articulos.Where(a => a.Usuario == usuario).ToList();
                if (indice < 1 || indice > articulosUsuario.Count)
                    return "Índice fuera de rango";

                var articulo = articulosUsuario[indice - 1];

                if (articulo.Ofertas.Any())
                    return "No se puede eliminar un artículo con ofertas registradas";
                if (articulo.FechaCierre <= DateTime.Now)
                    return "No se puede eliminar un remate finalizado";

                _articulos.Remove(articulo);

                if (!string.IsNullOrEmpty(articulo.ImagenNombreArchivo))
                {
                    try { File.Delete(articulo.ImagenNombreArchivo); } catch { }
                }
            }

            eliminado = true;
            return "Artículo eliminado correctamente";
        }

        private void ActualizarEstadoRemates()
        {
            lock (_lockArticulos)
            {
                foreach (var articulo in _articulos)
                {
                    if (!articulo.Finalizado && articulo.FechaCierre <= DateTime.Now)
                    {
                        articulo.Finalizado = true;
                        if (articulo.Ofertas.Any())
                        {
                            articulo.UsuarioGanador = articulo.Ofertas.OrderByDescending(o => o.Monto).First().Usuario;
                        }
                    }
                }
            }
        }

        public string ObtenerTodosLosArticulos()
        {
            ActualizarEstadoRemates();

            lock (_lockArticulos)
            {
                if (!_articulos.Any()) return "SIN_ARTICULOS";

                var sb = new StringBuilder();
                for (int i = 0; i < _articulos.Count; i++)
                {
                    var a = _articulos[i];
                    sb.AppendLine($"{i + 1}. {a.Titulo} | Descripción: {a.Descripcion} | Categoría: {a.Categoria} | Precio: {a.PrecioBase} | Cierra: {a.FechaCierre:dd-MM-yyyy HH:mm} | Estado: {(a.Finalizado ? "Finalizado" : "Activo")}");
                }
                return sb.ToString();
            }
        }

        public string ListarArticulosConImagen()
        {
            lock (_lockArticulos)
            {
                var conImagen = _articulos.Where(a => !string.IsNullOrEmpty(a.ImagenNombreArchivo)).ToList();
                if (!conImagen.Any()) return "SIN_IMAGENES";

                var sb = new StringBuilder();
                for (int i = 0; i < conImagen.Count; i++)
                {
                    var a = conImagen[i];
                    sb.AppendLine($"{i + 1}. {a.Titulo} | Imagen: {a.ImagenNombreArchivo}");
                }
                return sb.ToString();
            }
        }

        public string? ObtenerNombreArchivoImagen(int index)
        {
            lock (_lockArticulos)
            {
                var conImagen = _articulos.Where(a => !string.IsNullOrEmpty(a.ImagenNombreArchivo)).ToList();
                if (index >= 0 && index < conImagen.Count)
                {
                    return conImagen[index].ImagenNombreArchivo;
                }
                return null;
            }
        }
    }
}
