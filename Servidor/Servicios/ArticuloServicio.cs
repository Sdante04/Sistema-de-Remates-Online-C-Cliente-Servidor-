using Servidor.Dominio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Servidor.Servicios
{
    public class ArticuloServicio
    {
        private List<Articulo> _articulos = new();
        private readonly object _lock = new object();
        private int _ultimoId = 0;

        private const int MaxStringLength = 100;
        private const int StringByteSize = MaxStringLength * 4;
        private const string ArticulosFilePath = "articulos.bin";
        private const string OfertasFilePath = "ofertas.bin";
        private const string RematesFilePath = "remates.bin";

        private struct ArticuloLocal
        {
            public int ID;
            public byte[] TituloBytes;
            public byte[] DescripcionBytes;
            public byte[] CategoriaBytes;
            public int PrecioBase;
            public long FechaCierreTicks;
            public byte[] ImagenNombreArchivoBytes;
            public byte[] UsuarioBytes;
            public bool Finalizado;
            public byte[] UsuarioGanadorBytes;
        }

        private struct OfertaLocal
        {
            public int ArticuloID;
            public byte[] UsuarioBytes;
            public int Monto;
            public long FechaTicks;
        }

        private struct RemateLocal
        {
            public int ArticuloID;
            public byte[] TituloBytes;
            public int PrecioFinal;
            public byte[] UsuarioGanadorBytes;
            public long FechaCierreTicks;
        }

        public ArticuloServicio()
        {
            CargarArticulosDesdeArchivo();
            CargarOfertasDesdeArchivo();
            CargarRematesDesdeArchivo();
        }

        private void CargarArticulosDesdeArchivo()
        {
            lock (_lock)
            {
                _articulos.Clear();
                if (!File.Exists(ArticulosFilePath))
                {
                    Console.WriteLine("Archivo articulos.bin no encontrado. No se cargaron artículos.");
                    return;
                }

                try
                {
                    using (FileStream fs = new FileStream(ArticulosFilePath, FileMode.Open, FileAccess.Read))
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        int recordSize = sizeof(int) + 6 * StringByteSize + sizeof(int) + sizeof(long) + sizeof(bool);
                        int recordCount = (int)(fs.Length / recordSize);

                        for (int i = 0; i < recordCount; i++)
                        {
                            fs.Seek(i * recordSize, SeekOrigin.Begin);

                            ArticuloLocal articuloLocal = new ArticuloLocal
                            {
                                ID = reader.ReadInt32(),
                                TituloBytes = reader.ReadBytes(StringByteSize),
                                DescripcionBytes = reader.ReadBytes(StringByteSize),
                                CategoriaBytes = reader.ReadBytes(StringByteSize),
                                PrecioBase = reader.ReadInt32(),
                                FechaCierreTicks = reader.ReadInt64(),
                                ImagenNombreArchivoBytes = reader.ReadBytes(StringByteSize),
                                UsuarioBytes = reader.ReadBytes(StringByteSize),
                                Finalizado = reader.ReadBoolean(),
                                UsuarioGanadorBytes = reader.ReadBytes(StringByteSize)
                            };

                            if (articuloLocal.ID <= 0)
                            {
                                Console.WriteLine($"Advertencia: Artículo con ID inválido ({articuloLocal.ID}) en el registro {i}. Se omitirá.");
                                continue;
                            }

                            DateTime fechaCierre;
                            try
                            {
                                fechaCierre = new DateTime(articuloLocal.FechaCierreTicks);
                                if (fechaCierre.Year < 2000 || fechaCierre.Year > 2100)
                                {
                                    Console.WriteLine($"Advertencia: Fecha de cierre inválida ({fechaCierre}) en el registro {i}. Se omitirá.");
                                    continue;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                Console.WriteLine($"Advertencia: Fecha de cierre corrupta en el registro {i}. Se omitirá.");
                                continue;
                            }

                            Articulo articulo = new Articulo
                            {
                                ID = articuloLocal.ID,
                                Titulo = DecodeByteArrayToString(articuloLocal.TituloBytes),
                                Descripcion = DecodeByteArrayToString(articuloLocal.DescripcionBytes),
                                Categoria = DecodeByteArrayToString(articuloLocal.CategoriaBytes),
                                PrecioBase = articuloLocal.PrecioBase,
                                FechaCierre = fechaCierre,
                                ImagenNombreArchivo = DecodeByteArrayToString(articuloLocal.ImagenNombreArchivoBytes),
                                Usuario = DecodeByteArrayToString(articuloLocal.UsuarioBytes),
                                Finalizado = articuloLocal.Finalizado,
                                UsuarioGanador = DecodeByteArrayToString(articuloLocal.UsuarioGanadorBytes),
                                Ofertas = new List<Oferta>()
                            };

                            _articulos.Add(articulo);

                            if (articulo.ID > _ultimoId)
                            {
                                _ultimoId = articulo.ID;
                            }
                        }

                        Console.WriteLine($"Cargados {_articulos.Count} artículos desde {ArticulosFilePath}.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cargando artículos desde el archivo: {ex.Message}");
                }
            }
        }

        private void CargarOfertasDesdeArchivo()
        {
            lock (_lock)
            {
                if (!File.Exists(OfertasFilePath))
                {
                    Console.WriteLine("Archivo ofertas.bin no encontrado. No se cargaron ofertas.");
                    return;
                }

                try
                {
                    using (FileStream fs = new FileStream(OfertasFilePath, FileMode.Open, FileAccess.Read))
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        int recordSize = sizeof(int) + StringByteSize + sizeof(int) + sizeof(long);
                        int recordCount = (int)(fs.Length / recordSize);

                        for (int i = 0; i < recordCount; i++)
                        {
                            fs.Seek(i * recordSize, SeekOrigin.Begin);

                            int articuloID = reader.ReadInt32();
                            string usuario = DecodeByteArrayToString(reader.ReadBytes(StringByteSize));
                            int monto = reader.ReadInt32();
                            long fechaTicks = reader.ReadInt64();
                            DateTime fecha = new DateTime(fechaTicks);

                            var articulo = _articulos.FirstOrDefault(a => a.ID == articuloID);
                            if (articulo != null)
                            {
                                articulo.Ofertas.Add(new Oferta
                                {
                                    ArticuloID = articuloID,
                                    Usuario = usuario,
                                    Monto = monto,
                                    Fecha = fecha
                                });
                            }
                        }
                    }

                    Console.WriteLine("Ofertas cargadas correctamente desde ofertas.bin.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cargando ofertas: {ex.Message}");
                }
            }
        }

        private void CargarRematesDesdeArchivo()
        {
            lock (_lock)
            {
                if (!File.Exists(RematesFilePath))
                {
                    Console.WriteLine("Archivo remates.bin no encontrado. No se cargaron remates.");
                    return;
                }

                try
                {
                    using (FileStream fs = new FileStream(RematesFilePath, FileMode.Open, FileAccess.Read))
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        int recordSize = sizeof(int) + StringByteSize + sizeof(int) + StringByteSize + sizeof(long);
                        int recordCount = (int)(fs.Length / recordSize);

                        for (int i = 0; i < recordCount; i++)
                        {
                            fs.Seek(i * recordSize, SeekOrigin.Begin);

                            int articuloID = reader.ReadInt32();
                            string titulo = DecodeByteArrayToString(reader.ReadBytes(StringByteSize));
                            int precioFinal = reader.ReadInt32();
                            string usuarioGanador = DecodeByteArrayToString(reader.ReadBytes(StringByteSize));
                            long fechaCierreTicks = reader.ReadInt64();

                            var articulo = _articulos.FirstOrDefault(a => a.ID == articuloID);
                            if (articulo != null)
                            {
                                articulo.Finalizado = true;
                                articulo.UsuarioGanador = usuarioGanador;
                                articulo.FechaCierre = new DateTime(fechaCierreTicks);
                            }
                        }
                    }

                    Console.WriteLine("Remates cargados correctamente desde remates.bin.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cargando remates: {ex.Message}");
                }
            }
        }

        private void GuardarArticulosEnArchivo()
        {
            lock (_lock)
            {
                try
                {
                    if (!_articulos.Any())
                    {
                        if (File.Exists(ArticulosFilePath))
                        {
                            File.Delete(ArticulosFilePath);
                            Console.WriteLine("No hay artículos para guardar. Archivo articulos.bin eliminado.");
                        }
                        return;
                    }

                    string directory = Path.GetDirectoryName(ArticulosFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (FileStream fs = new FileStream(ArticulosFilePath, FileMode.Create, FileAccess.Write))
                    using (BinaryWriter writer = new BinaryWriter(fs))
                    {
                        foreach (var articulo in _articulos)
                        {
                            ArticuloLocal articuloLocal = new ArticuloLocal
                            {
                                ID = articulo.ID,
                                TituloBytes = EncodeStringToFixedSizeByteArray(articulo.Titulo, StringByteSize),
                                DescripcionBytes = EncodeStringToFixedSizeByteArray(articulo.Descripcion, StringByteSize),
                                CategoriaBytes = EncodeStringToFixedSizeByteArray(articulo.Categoria, StringByteSize),
                                PrecioBase = articulo.PrecioBase,
                                FechaCierreTicks = articulo.FechaCierre.Ticks,
                                ImagenNombreArchivoBytes = EncodeStringToFixedSizeByteArray(articulo.ImagenNombreArchivo ?? "", StringByteSize),
                                UsuarioBytes = EncodeStringToFixedSizeByteArray(articulo.Usuario, StringByteSize),
                                Finalizado = articulo.Finalizado,
                                UsuarioGanadorBytes = EncodeStringToFixedSizeByteArray(articulo.UsuarioGanador ?? "", StringByteSize)
                            };

                            writer.Write(articuloLocal.ID);
                            writer.Write(articuloLocal.TituloBytes);
                            writer.Write(articuloLocal.DescripcionBytes);
                            writer.Write(articuloLocal.CategoriaBytes);
                            writer.Write(articuloLocal.PrecioBase);
                            writer.Write(articuloLocal.FechaCierreTicks);
                            writer.Write(articuloLocal.ImagenNombreArchivoBytes);
                            writer.Write(articuloLocal.UsuarioBytes);
                            writer.Write(articuloLocal.Finalizado);
                            writer.Write(articuloLocal.UsuarioGanadorBytes);
                        }

                        writer.Flush();
                        fs.Flush();
                    }

                    Console.WriteLine($"Artículos guardados correctamente. Total: {_articulos.Count} artículos.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error guardando artículos: {ex.Message}");
                    throw;
                }
            }
        }

        public string PublicarArticulo(string datos, string usuario, out bool esExitoso)
        {
            lock (_lock)
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
                    ID = ++_ultimoId,
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

                GuardarArticulosEnArchivo();

                return $"El artículo '{titulo}' fue publicado correctamente.|ID={nuevo.ID}";
            }
        }

        public bool ValidarDatosArticulo(string datos)
        {
            var partes = datos.Split('|');
            if (partes.Length < 5)
                return false;

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
            lock (_lock)
            {
                ActualizarEstadoRemates();

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

        public string ObtenerTodosLosArticulosEnRemate()
        {
            lock (_lock)
            {
                ActualizarEstadoRemates();

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

        public string EditarArticulo(string datos, string usuario, out bool exito)
        {
            exito = false;
            var partes = datos.Split('|');
            if (partes.Length < 7)
                return "Datos insuficientes para editar.";

                if (!int.TryParse(partes[0], out int id))
                    return "ID inválido.";

                var articulo = _articulos.FirstOrDefault(a => a.ID == id && a.Usuario == usuario);
                if (articulo == null)
                    return "Artículo no encontrado o no tienes permiso para editarlo.";

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

        public string RealizarOferta(string datos, string usuario)
        {
            lock (_lock)
            {
                var partes = datos.Split('|');
                if (partes.Length != 2)
                    return "Datos inválidos. Se esperaba: id|monto";

            if (!int.TryParse(partes[0], out int indice))
                return "Índice inválido.";
            if (!int.TryParse(partes[1], out int montoOfertado))
                return "Monto inválido.";

            var articulosEnRemate = _articulos.Where(a => a.FechaCierre > DateTime.Now).ToList();
            if (indice < 1 || indice > articulosEnRemate.Count)
                return "Índice fuera de rango.";

            var articulo = articulosEnRemate[indice - 1];
            int ofertaMaxima = articulo.Ofertas.Any() ? articulo.Ofertas.Max(o => o.Monto) : articulo.PrecioBase;

            if (montoOfertado < articulo.PrecioBase)
                return "La oferta es menor al precio base.";
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

        public string ConsultarArticulo(string idStr)
        {
            lock (_lock)
            {
                ActualizarEstadoRemates();

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
            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays} días, {ts.Hours} horas";
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours} horas, {ts.Minutes} minutos";
            return $"{ts.Minutes} minutos, {ts.Seconds} segundos";
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
            foreach (var articulo in _articulos)
            {
                if (!articulo.Finalizado && articulo.FechaCierre <= DateTime.Now)
                {
                    articulo.Finalizado = true;

                    if (articulo.Ofertas.Any())
                    {
                        var mejorOferta = articulo.Ofertas.OrderByDescending(o => o.Monto).First();
                        articulo.UsuarioGanador = mejorOferta.Usuario;
                    }
                }
            }
        }

        public string ObtenerTodosLosArticulos()
        {
            lock (_lock)
            {
                ActualizarEstadoRemates();

            if (!_articulos.Any()) return "SIN_ARTICULOS";

            var sb = new StringBuilder();
            for (int i = 0; i < _articulos.Count; i++)
            {
                var a = _articulos[i];
                sb.AppendLine($"{i + 1}. {a.Titulo} | Descripción: {a.Descripcion} | Categoría: {a.Categoria} | Precio: {a.PrecioBase} | Cierra: {a.FechaCierre:dd-MM-yyyy HH:mm} | Estado: {(a.Finalizado ? "Finalizado" : "Activo")}");
            }

            return sb.ToString();
        }

        public string EliminarArticulo(string datos, string usuario, out bool eliminado)
        {
            eliminado = false;

            if (!int.TryParse(datos, out int indice))
                return "Índice inválido.";

            var articulosUsuario = _articulos.Where(a => a.Usuario == usuario).ToList();
            if (indice < 1 || indice > articulosUsuario.Count)
                return "Índice fuera de rango.";

            var articulo = articulosUsuario[indice - 1];

            if (articulo.Ofertas.Any())
                return "No se puede eliminar un artículo con ofertas registradas.";

            if (articulo.FechaCierre <= DateTime.Now)
                return "No se puede eliminar un remate finalizado.";

            _articulos.Remove(articulo);
            eliminado = true;

            if (!string.IsNullOrEmpty(articulo.ImagenNombreArchivo))
            {
                try
                {
                    File.Delete(articulo.ImagenNombreArchivo);
                }
                catch { }
            }

            return $"Artículo '{articulo.Titulo}' eliminado correctamente.";
        }

        public string ListarArticulosConImagen()
        {
            var conImagen = _articulos.Where(a => !string.IsNullOrEmpty(a.ImagenNombreArchivo)).ToList();
            if (!conImagen.Any())
                return "SIN_IMAGENES";

            var sb = new StringBuilder();
            for (int i = 0; i < conImagen.Count; i++)
            {
                var a = conImagen[i];
                sb.AppendLine($"{i + 1}. {a.Titulo} | Imagen: {a.ImagenNombreArchivo}");
            }
            return sb.ToString();
        }

        public string? ObtenerNombreArchivoImagen(int index)
        {
            var conImagen = _articulos.Where(a => !string.IsNullOrEmpty(a.ImagenNombreArchivo)).ToList();
            if (index >= 0 && index < conImagen.Count)
            {
                return conImagen[index].ImagenNombreArchivo;
            }
            return null;
        }


        private static string DecodeByteArrayToString(byte[] byteArray)
        {
            int actualLength = Array.IndexOf(byteArray, (byte)0);
            if (actualLength < 0) actualLength = byteArray.Length;
            return Encoding.UTF8.GetString(byteArray, 0, actualLength).TrimEnd('\0');
        }
    }
}
