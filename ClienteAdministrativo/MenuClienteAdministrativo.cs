using Grpc.Core;
using Remate.GRPC;
using System.Text;

namespace ClienteAdministrativo
{
    public class MenuClienteAdministrativo
    {
        private readonly Administracion.AdministracionClient _client;

        private const int MaxStringLength = 100;
        private const int StringByteSize = MaxStringLength * 4;
        private const string ArticulosFilePath = "articulos.bin";
        private const string OfertasFilePath = "ofertas.bin";
        private const string RematesFilePath = "remates.bin";
        private const string UsuariosFilePath = "usuarios.bin";

        private string _usuarioActual = "administrador";

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
        }

        private struct OfertaLocal
        {
            public int ArticuloID;
            public byte[] UsuarioBytes;
            public int Monto;
            public long FechaTicks;
        }

        private struct RemateGanadoLocal
        {
            public int ArticuloID;
            public byte[] TituloBytes;
            public int PrecioFinal;
            public byte[] UsuarioGanadorBytes;
            public long FechaCierreTicks;
        }

        private struct UsuarioLocal
        {
            public byte[] NombreUsuarioBytes;
            public byte[] ClaveBytes;
        }

        private int RecordSize => sizeof(int)
                        + StringByteSize
                        + StringByteSize
                        + StringByteSize
                        + sizeof(int)
                        + sizeof(long)
                        + StringByteSize
                        + StringByteSize
                        + sizeof(bool);



        public MenuClienteAdministrativo(Administracion.AdministracionClient client)
        {
            _client = client
                ?? throw new ArgumentNullException(nameof(client), "El cliente gRPC no puede ser null");
        }


        public async Task IniciarAsync()
        {
            var articulos = LeerTodosArticulosLocales();
            Console.WriteLine($"Artículos locales: {articulos.Count}");

            var ofertas = LeerTodasOfertasLocales();
            Console.WriteLine($"Ofertas locales: {ofertas.Count}");

            var remates = LeerTodosRematesLocales();
            Console.WriteLine($"Remates ganados locales: {remates.Count}");

            var usuarios = LeerTodosUsuariosLocales();
            Console.WriteLine($"Usuarios locales: {usuarios.Count}");

            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(" ═════════════════════════════");
                Console.WriteLine("║  MENÚ CLIENTE ADMINISTRADOR ║");
                Console.WriteLine(" ═════════════════════════════");
                Console.ResetColor();

                Console.WriteLine("1. ABM de Artículos");
                Console.WriteLine("2. Consultar historial de usuario");
                Console.WriteLine("3. Ver próximos N inicios de sesión");
                Console.WriteLine("0. Salir");
                Console.Write("Selecciona una opción: ");

                var input = Console.ReadLine();

                if (!int.TryParse(input, out int opcion))
                {
                    Console.WriteLine("Opción inválida, por favor ingresa un número.");
                    Console.WriteLine("Presiona una tecla para continuar...");
                    Console.ReadKey();
                    continue;
                }

                switch (opcion)
                {
                    case 1:
                        await ABMArticuloAsync();
                        break;
                    case 2:
                        await ConsultarHistorialAsync();
                        break;
                    case 3:
                        await VerIniciosSesionAsync();
                        break;
                    case 0:
                        Console.WriteLine("Conexión terminada.");
                        Console.WriteLine("Presiona una tecla para salir...");
                        Console.ReadKey();
                        return;
                    default:
                        Console.WriteLine("Opción inválida.");
                        break;
                }

                Console.WriteLine("Presiona una tecla para continuar...");
                Console.ReadKey();
            }
        }


        private async Task ABMArticuloAsync()
        {
            Console.WriteLine("¿Qué operación deseas realizar sobre un artículo?");
            Console.WriteLine("1. Alta");
            Console.WriteLine("2. Baja");
            Console.WriteLine("3. Modificación");
            Console.WriteLine("0. Cancelar");
            Console.Write("Selecciona una opción: ");
            string opcion = Console.ReadLine();

            switch (opcion)
            {
                case "1":
                    await PublicarArticuloGrpc();
                    break;
                case "2":
                    await EliminarArticuloGrpc();
                    break;
                case "3":
                    await EditarArticuloGrpc();
                    break;
                case "0":
                    Console.WriteLine("Operación cancelada.");
                    break;
                default:
                    Console.WriteLine("Opción inválida.");
                    break;
            }
        }

        private async Task PublicarArticuloGrpc()
        {
            Console.Write("Título: ");
            string titulo = Console.ReadLine();
            Console.Write("Descripción: ");
            string descripcion = Console.ReadLine();
            Console.Write("Categoría: ");
            string categoria = Console.ReadLine();
            Console.Write("Precio base: ");
            string precio = Console.ReadLine();
            Console.Write("Fecha cierre (dd-MM-yyyy HH:mm): ");
            string fecha = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(descripcion) || string.IsNullOrWhiteSpace(categoria))
            {
                Console.WriteLine("Título, descripción y categoría son obligatorios.");
                return;
            }
            if (!int.TryParse(precio, out int precioBase))
            {
                Console.WriteLine("El precio base debe ser un número entero.");
                return;
            }
            if (!DateTime.TryParseExact(fecha, "dd-MM-yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime fechaCierre))
            {
                Console.WriteLine("Fecha de cierre inválida. Usa el formato dd-MM-yyyy HH:mm.");
                return;
            }

            string imagen = "";
            Console.Write("¿Deseas agregar una imagen? (S/N): ");
            string agregarImagen = Console.ReadLine()?.Trim().ToUpper();
            if (agregarImagen == "S")
            {
                Console.Write("Ruta de la imagen: ");
                string ruta = Console.ReadLine();
                if (File.Exists(ruta))
                {
                    try
                    {
                        await EnviarArchivoPorPartesAsync(ruta);
                        imagen = Path.GetFileName(ruta);
                    }
                    catch
                    {
                        Console.WriteLine("Error al leer o enviar el archivo. Se continuará sin imagen.");
                    }
                }
                else
                {
                    Console.WriteLine("Archivo no encontrado. Se continuará sin imagen.");
                }
            }

            var request = new ABMArticuloRequest
            {
                Operacion = "alta",
                Titulo = titulo,
                Descripcion = descripcion,
                Categoria = categoria,
                PrecioBase = precioBase,
                FechaCierre = fecha,
                ImagenNombreArchivo = imagen,
                Usuario = _usuarioActual
            };

            var response = await _client.ABMArticuloAsync(request);
            Console.WriteLine($"Respuesta: {response.Mensaje}");

            if (!response.Mensaje.Contains("fue publicado correctamente"))
            {
                Console.WriteLine($"Error al publicar: {response.Mensaje}");
                return;
            }

            try
            {
                int id = 0;
                var partes = response.Mensaje.Split('|');
                foreach (var parte in partes)
                {
                    if (parte.Trim().StartsWith("id="))
                    {
                        if (int.TryParse(parte.Trim().Substring(3), out int idParseado))
                        {
                            id = idParseado;
                            break;
                        }
                    }
                }

                ArticuloLocal articulo = new ArticuloLocal
                {
                    ID = id,
                    TituloBytes = EncodeStringToFixedSizeByteArray(titulo, StringByteSize),
                    DescripcionBytes = EncodeStringToFixedSizeByteArray(descripcion, StringByteSize),
                    CategoriaBytes = EncodeStringToFixedSizeByteArray(categoria, StringByteSize),
                    PrecioBase = precioBase,
                    FechaCierreTicks = fechaCierre.Ticks,
                    ImagenNombreArchivoBytes = EncodeStringToFixedSizeByteArray(imagen, StringByteSize),
                    UsuarioBytes = EncodeStringToFixedSizeByteArray(_usuarioActual, StringByteSize),
                    Finalizado = false
                };

                if (!ArticuloYaExiste(id))
                {
                    GuardarArticuloLocal(articulo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando artículo localmente: {ex.Message}");
            }
        }

        private async Task EliminarArticuloGrpc()
        {
            if (_client == null)
            {
                Console.WriteLine("[ERROR] _client es null en EliminarArticuloGrpc()");
                return;
            }

            var requestListado = new ABMArticuloRequest
            {
                Operacion = "listar"
            };

            var responseListado = await _client.ABMArticuloAsync(requestListado);

            if (responseListado.Mensaje == "SIN_ARTICULOS")
            {
                Console.WriteLine("No hay artículos disponibles para eliminar.");
                return;
            }

            var articulos = responseListado.Mensaje
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (articulos.Count == 0)
            {
                Console.WriteLine("No hay artículos eliminables en este momento.");
                return;
            }

            Console.WriteLine("Artículos disponibles para eliminar:");
            foreach (var art in articulos)
            {
                Console.WriteLine(art);
            }

            Console.Write("Selecciona el ID del artículo a eliminar: ");
            string seleccion = Console.ReadLine();
            if (!int.TryParse(seleccion, out int id))
            {
                Console.WriteLine("ID inválido.");
                return;
            }

            var requestEliminar = new ABMArticuloRequest
            {
                Operacion = "baja",
                Id = id,
                Usuario = _usuarioActual
            };

            var response = await _client.ABMArticuloAsync(requestEliminar);
            Console.WriteLine(response.Mensaje);
        }


        private async Task EditarArticuloGrpc()
        {
            var requestListado = new ABMArticuloRequest
            {
                Operacion = "listar"
            };

            var responseListado = await _client.ABMArticuloAsync(requestListado);

            if (responseListado.Mensaje == "SIN_ARTICULOS")
            {
                Console.WriteLine("No hay artículos para editar.");
                return;
            }

            Console.WriteLine("Artículos disponibles:");
            Console.WriteLine(responseListado.Mensaje);

            Console.Write("Selecciona el ID del artículo a editar (o 0 para volver): ");
            if (!int.TryParse(Console.ReadLine(), out int id) || id == 0)
                return;

            Console.WriteLine("¿Qué deseas editar?");
            Console.WriteLine("1. Título");
            Console.WriteLine("2. Descripción");
            Console.WriteLine("3. Categoría");
            Console.WriteLine("4. Precio base");
            Console.WriteLine("5. Fecha de cierre");
            Console.WriteLine("6. Imagen");
            Console.WriteLine("0. Finalizar sin editar");

            string nuevoTitulo = "", descripcion = "", categoria = "", precio = "", fecha = "", imagen = "";

            while (true)
            {
                Console.Write("Selecciona opción a editar (0 para finalizar): ");
                string opcion = Console.ReadLine();
                if (opcion == "0") break;

                switch (opcion)
                {
                    case "1":
                        Console.Write("Nuevo título: ");
                        nuevoTitulo = Console.ReadLine();
                        break;
                    case "2":
                        Console.Write("Nueva descripción: ");
                        descripcion = Console.ReadLine();
                        break;
                    case "3":
                        Console.Write("Nueva categoría: ");
                        categoria = Console.ReadLine();
                        break;
                    case "4":
                        Console.Write("Nuevo precio base: ");
                        precio = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(precio) && !int.TryParse(precio, out _))
                        {
                            Console.WriteLine("El precio base debe ser un número entero.");
                            precio = "";
                        }
                        break;
                    case "5":
                        Console.Write("Nueva fecha cierre (dd-MM-yyyy HH:mm): ");
                        fecha = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(fecha) && !DateTime.TryParseExact(fecha, "dd-MM-yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out _))
                        {
                            Console.WriteLine("Fecha inválida.");
                            fecha = "";
                        }
                        break;
                    case "6":
                        Console.Write("Ruta de la nueva imagen: ");
                        string ruta = Console.ReadLine();
                        if (File.Exists(ruta))
                        {
                            await EnviarArchivoPorPartesAsync(ruta);
                            imagen = Path.GetFileName(ruta);
                        }
                        else
                        {
                            Console.WriteLine("Archivo no encontrado. Se omitirá la imagen.");
                        }
                        break;
                    default:
                        Console.WriteLine("Opción inválida.");
                        break;
                }
            }

            var request = new ABMArticuloRequest
            {
                Operacion = "modificacion",
                Id = id,
                Titulo = nuevoTitulo,
                Descripcion = descripcion,
                Categoria = categoria,
                PrecioBase = string.IsNullOrWhiteSpace(precio) ? 0 : int.Parse(precio),
                FechaCierre = fecha,
                ImagenNombreArchivo = imagen,
                Usuario = _usuarioActual
            };

            var response = await _client.ABMArticuloAsync(request);
            Console.WriteLine(response.Mensaje);

            if (response.Mensaje.Contains("editado correctamente"))
            {
                try
                {
                    var articulosLocales = LeerTodosArticulosLocales();
                    var articulo = articulosLocales.FirstOrDefault(a => a.ID == id);
                    if (articulo.ID != 0)
                    {
                        if (!string.IsNullOrWhiteSpace(nuevoTitulo)) articulo.TituloBytes = EncodeStringToFixedSizeByteArray(nuevoTitulo, StringByteSize);
                        if (!string.IsNullOrWhiteSpace(descripcion)) articulo.DescripcionBytes = EncodeStringToFixedSizeByteArray(descripcion, StringByteSize);
                        if (!string.IsNullOrWhiteSpace(categoria)) articulo.CategoriaBytes = EncodeStringToFixedSizeByteArray(categoria, StringByteSize);
                        if (!string.IsNullOrWhiteSpace(precio) && int.TryParse(precio, out int nuevoPrecio)) articulo.PrecioBase = nuevoPrecio;
                        if (!string.IsNullOrWhiteSpace(fecha) && DateTime.TryParseExact(fecha, "dd-MM-yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime nuevaFecha)) articulo.FechaCierreTicks = nuevaFecha.Ticks;
                        if (!string.IsNullOrWhiteSpace(imagen)) articulo.ImagenNombreArchivoBytes = EncodeStringToFixedSizeByteArray(imagen, StringByteSize);

                        ActualizarArticuloLocal(id, articulo);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error actualizando artículo localmente: {ex.Message}");
                }
            }
        }

        private void ActualizarArticuloLocal(int id, ArticuloLocal articuloActualizado)
        {
            var articulos = LeerTodosArticulosLocales();
            int index = articulos.FindIndex(a => a.ID == id);

            if (index >= 0)
            {
                articulos[index] = articuloActualizado;

                try
                {
                    File.Delete(ArticulosFilePath);
                    foreach (var articulo in articulos)
                    {
                        GuardarArticuloLocal(articulo);
                    }

                    Console.WriteLine($"Artículo con ID {id} actualizado localmente.");
                    Console.WriteLine($"Archivo reescrito. Total artículos: {articulos.Count}, tamaño total: {articulos.Count * RecordSize} bytes.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error actualizando archivo binario: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"No se encontró ningún artículo con ID {id} para actualizar.");
            }
        }


        private async Task ConsultarHistorialAsync()
        {
            string usuario;
            HistorialResponse response;

            int cursorTopInicial = Console.CursorTop;

            while (true)
            {
                Console.Write("Nombre del usuario para consultar historial (0 para salir): ");
                usuario = Console.ReadLine()?.Trim();

                if (usuario == "0")
                {
                    Console.WriteLine("Saliendo de la consulta de historial...");
                    return; // Termina el método
                }

                if (string.IsNullOrWhiteSpace(usuario))
                {
                    Console.WriteLine("Usuario inexistente, vuelva a escribir.");
                    Console.WriteLine("Presiona una tecla para intentar nuevamente...");
                    Console.ReadKey();

                    int cursorActual = Console.CursorTop;
                    for (int i = cursorTopInicial; i < cursorActual; i++)
                    {
                        Console.SetCursorPosition(0, i);
                        Console.Write(new string(' ', Console.WindowWidth));
                    }

                    Console.SetCursorPosition(0, cursorTopInicial);
                    continue;
                }

                var request = new HistorialRequest { NombreUsuario = usuario };
                response = await _client.ConsultarHistorialAsync(request);

                if (response.Actividades.Count == 1 && response.Actividades[0].StartsWith("ERROR: El usuario"))
                {
                    Console.WriteLine("Usuario inexistente, vuelva a escribir.");
                    Console.WriteLine("Presiona una tecla para intentar nuevamente...");
                    Console.ReadKey();

                    int cursorActual = Console.CursorTop;
                    for (int i = cursorTopInicial; i < cursorActual; i++)
                    {
                        Console.SetCursorPosition(0, i);
                        Console.Write(new string(' ', Console.WindowWidth));
                    }

                    Console.SetCursorPosition(0, cursorTopInicial);
                    continue;
                }

                break;
            }

            Console.WriteLine("\n=== Historial de Actividades ===");

            var actividades = response.Actividades;

            Console.WriteLine("\nArtículos publicados:");
            var publicados = actividades.Where(a => a.StartsWith("PUBLICADO")).ToList();
            if (publicados.Count == 0)
                Console.WriteLine("No ha publicado artículos.");
            else
                publicados.ForEach(p => Console.WriteLine(p));

            Console.WriteLine("\nOfertas realizadas:");
            var ofertas = actividades.Where(a => a.StartsWith("OFERTA")).ToList();
            if (ofertas.Count == 0)
                Console.WriteLine("No ha realizado ofertas.");
            else
                ofertas.ForEach(o => Console.WriteLine(o));

            Console.WriteLine("\nRemates ganados:");
            var remates = actividades.Where(a => a.StartsWith("REMATE_GANADO")).ToList();
            if (remates.Count == 0 || remates.Any(r => r.Contains("SIN_REMATES")))
            {
                Console.WriteLine("No ha ganado ningún remate.");
            }
            else
            {
                foreach (var linea in remates)
                {
                    Console.WriteLine(linea);

                    if (linea.Contains("SIN_REMATES"))
                        continue;

                    try
                    {
                        var partes = linea.Substring("REMATE_GANADO".Length).Trim().Split(',');

                        int articuloId = int.Parse(partes[0].Split('=')[1]);
                        string titulo = partes[1].Split('=')[1].Trim();
                        int precioFinal = int.Parse(partes[2].Split('=')[1]);
                        DateTime fechaCierre = DateTime.ParseExact(partes[3].Split('=')[1].Trim(), "dd-MM-yyyy HH:mm", null);

                        RemateGanadoLocal remate = new RemateGanadoLocal
                        {
                            ArticuloID = articuloId,
                            TituloBytes = EncodeStringToFixedSizeByteArray(titulo, StringByteSize),
                            PrecioFinal = precioFinal,
                            UsuarioGanadorBytes = EncodeStringToFixedSizeByteArray(usuario, StringByteSize),
                            FechaCierreTicks = fechaCierre.Ticks
                        };

                        if (!RemateYaExiste(articuloId))
                        {
                            GuardarRemateGanadoLocal(remate);
                            Console.WriteLine($"Remate guardado localmente (ID: {articuloId})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error procesando remate: {ex.Message}");
                    }
                }
            }
        }



        private bool RemateYaExiste(int id)
        {
            var remates = LeerTodosRematesLocales();
            return remates.Any(r => r.ArticuloID == id);
        }

        private void GuardarRemateGanadoLocal(RemateGanadoLocal remate)
        {
            try
            {
                using (FileStream fs = new FileStream(RematesFilePath, FileMode.Append, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    writer.Write(remate.ArticuloID);
                    writer.Write(remate.TituloBytes);
                    writer.Write(remate.PrecioFinal);
                    writer.Write(remate.UsuarioGanadorBytes);
                    writer.Write(remate.FechaCierreTicks);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando remate localmente: {ex.Message}");
            }
        }

        private async Task VerIniciosSesionAsync()
        {
            Console.Write("Teniendo en cuenta que esta accion no se podra cancelar, ingrese la cantidad de próximos inicios de sesión a ver: ");
            if (!int.TryParse(Console.ReadLine(), out int cantidad) || cantidad <= 0)
            {
                Console.WriteLine("Cantidad inválida.");
                return;
            }

            try
            {
                var request = new IniciosSesionRequest { Cantidad = cantidad };
                using var call = _client.VerProximosIniciosSesion(request);

                Console.WriteLine($"\nPróximos {cantidad} inicios de sesión de clientes normales:\n");

                await foreach (var inicio in call.ResponseStream.ReadAllAsync())
                {
                    Console.WriteLine($"• Usuario: {inicio.NombreUsuario} - Fecha y hora: {inicio.Timestamp}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] No se pudo recibir el stream: {ex.Message}");
            }
        }


        private async Task EnviarArchivoPorPartesAsync(string rutaArchivo)
        {
            try
            {
                byte[] datosArchivo = await File.ReadAllBytesAsync(rutaArchivo);

                var request = new SubirArchivoRequest
                {
                    NombreArchivo = Path.GetFileName(rutaArchivo),
                    Datos = Google.Protobuf.ByteString.CopyFrom(datosArchivo),
                    EsUltimaParte = true
                };

                var response = await _client.SubirArchivoAsync(request);

                if (response.Ok)
                {
                    Console.WriteLine("Archivo enviado correctamente.");
                }
                else
                {
                    Console.WriteLine($"Error al enviar archivo: {response.Mensaje}");
                }
            }
            catch (RpcException rpcEx)
            {
                Console.WriteLine($"Error en comunicación con el servidor gRPC: {rpcEx.Status.Detail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error leyendo o enviando archivo: {ex.Message}");
            }
        }

        private List<ArticuloLocal> LeerTodosArticulosLocales()
        {
            List<ArticuloLocal> articulos = new List<ArticuloLocal>();
            if (!File.Exists(ArticulosFilePath)) return articulos;

            using (FileStream fs = new FileStream(ArticulosFilePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int recordCount = (int)(fs.Length / RecordSize);
                for (int i = 0; i < recordCount; i++)
                {
                    fs.Seek(i * RecordSize, SeekOrigin.Begin);
                    ArticuloLocal articulo = new ArticuloLocal
                    {
                        ID = reader.ReadInt32(),
                        TituloBytes = reader.ReadBytes(StringByteSize),
                        DescripcionBytes = reader.ReadBytes(StringByteSize),
                        CategoriaBytes = reader.ReadBytes(StringByteSize),
                        PrecioBase = reader.ReadInt32(),
                        FechaCierreTicks = reader.ReadInt64(),
                        ImagenNombreArchivoBytes = reader.ReadBytes(StringByteSize),
                        UsuarioBytes = reader.ReadBytes(StringByteSize),
                        Finalizado = reader.ReadBoolean()
                    };
                    articulos.Add(articulo);
                }
            }
            return articulos;
        }


        private static byte[] EncodeStringToFixedSizeByteArray(string input, int fixedSize)
        {
            byte[] stringBytes = Encoding.UTF8.GetBytes(input ?? "");
            Array.Resize(ref stringBytes, fixedSize);
            return stringBytes;
        }

        private bool ArticuloYaExiste(int id)
        {
            if (!File.Exists(ArticulosFilePath)) return false;

            using (FileStream fs = new FileStream(ArticulosFilePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int recordCount = (int)(fs.Length / RecordSize);
                for (int i = 0; i < recordCount; i++)
                {
                    fs.Seek(i * RecordSize, SeekOrigin.Begin);
                    int existingId = reader.ReadInt32();
                    if (existingId == id) return true;
                }
            }
            return false;
        }


        private void GuardarArticuloLocal(ArticuloLocal articulo)
        {
            try
            {
                using (FileStream fs = new FileStream(ArticulosFilePath, FileMode.Append, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    writer.Write(articulo.ID);
                    writer.Write(articulo.TituloBytes);
                    writer.Write(articulo.DescripcionBytes);
                    writer.Write(articulo.CategoriaBytes);
                    writer.Write(articulo.PrecioBase);
                    writer.Write(articulo.FechaCierreTicks);
                    writer.Write(articulo.ImagenNombreArchivoBytes);
                    writer.Write(articulo.UsuarioBytes);
                    writer.Write(articulo.Finalizado);
                }
                Console.WriteLine("Artículo guardado localmente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando artículo localmente: {ex.Message}");
            }
        }


        private List<RemateGanadoLocal> LeerTodosRematesLocales()
        {
            List<RemateGanadoLocal> remates = new List<RemateGanadoLocal>();
            if (!File.Exists(RematesFilePath)) return remates;

            using (FileStream fs = new FileStream(RematesFilePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int recordSize = sizeof(int) + 2 * StringByteSize + sizeof(int) + sizeof(long);
                int recordCount = (int)(fs.Length / recordSize);
                for (int i = 0; i < recordCount; i++)
                {
                    fs.Seek(i * recordSize, SeekOrigin.Begin);
                    RemateGanadoLocal remate = new RemateGanadoLocal
                    {
                        ArticuloID = reader.ReadInt32(),
                        TituloBytes = reader.ReadBytes(StringByteSize),
                        PrecioFinal = reader.ReadInt32(),
                        UsuarioGanadorBytes = reader.ReadBytes(StringByteSize),
                        FechaCierreTicks = reader.ReadInt64()
                    };
                    remates.Add(remate);
                }
            }
            return remates;
        }

        private List<OfertaLocal> LeerTodasOfertasLocales()
        {
            List<OfertaLocal> ofertas = new List<OfertaLocal>();
            if (!File.Exists(OfertasFilePath)) return ofertas;

            using (FileStream fs = new FileStream(OfertasFilePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int recordSize = sizeof(int) + StringByteSize + sizeof(int) + sizeof(long);
                int recordCount = (int)(fs.Length / recordSize);
                for (int i = 0; i < recordCount; i++)
                {
                    fs.Seek(i * recordSize, SeekOrigin.Begin);
                    OfertaLocal oferta = new OfertaLocal
                    {
                        ArticuloID = reader.ReadInt32(),
                        UsuarioBytes = reader.ReadBytes(StringByteSize),
                        Monto = reader.ReadInt32(),
                        FechaTicks = reader.ReadInt64()
                    };
                    ofertas.Add(oferta);
                }
            }
            return ofertas;
        }

        private List<UsuarioLocal> LeerTodosUsuariosLocales()
        {
            List<UsuarioLocal> usuarios = new List<UsuarioLocal>();
            if (!File.Exists(UsuariosFilePath)) return usuarios;

            using (FileStream fs = new FileStream(UsuariosFilePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int recordSize = 2 * StringByteSize;
                int recordCount = (int)(fs.Length / recordSize);
                for (int i = 0; i < recordCount; i++)
                {
                    fs.Seek(i * recordSize, SeekOrigin.Begin);
                    UsuarioLocal usuario = new UsuarioLocal
                    {
                        NombreUsuarioBytes = reader.ReadBytes(StringByteSize),
                        ClaveBytes = reader.ReadBytes(StringByteSize)
                    };
                    usuarios.Add(usuario);
                }
            }
            return usuarios;
        }
    }
}
