using Common;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Cliente
{
    public class MenuCliente
    {
        private Cliente _cliente;
        private string _usuarioActual = string.Empty;

        private const int MaxStringLength = 100;
        private const int StringByteSize = MaxStringLength * 4;
        private const string ArticulosFilePath = "articulos.bin";
        private const string OfertasFilePath = "ofertas.bin";
        private const string RematesFilePath = "remates.bin";
        private const string UsuariosFilePath = "usuarios.bin";

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

        public MenuCliente(Cliente cliente)
        {
            _cliente = cliente;
        }

        public async Task MostrarAsync()
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine(" ═════════════════════════════");
                Console.WriteLine("║  MENÚ PRINCIPAL DE CLIENTE  ║");
                Console.WriteLine(" ═════════════════════════════");
                Console.ResetColor();

                Console.WriteLine(" 1.  Login");
                Console.WriteLine(" 2.  Registrar usuario");
                Console.WriteLine(" 3.  Publicar artículo");
                Console.WriteLine(" 4.  Editar artículo");
                Console.WriteLine(" 5.  Realizar oferta");
                Console.WriteLine(" 6.  Consultar artículo");
                Console.WriteLine(" 7.  Buscar artículos por categoría");
                Console.WriteLine(" 8.  Descargar imagen de artículo");
                Console.WriteLine(" 9.  Eliminar artículo");
                Console.WriteLine(" 10. Ver historial de actividades");
                Console.WriteLine(" 11. Consultar datos locales");
                Console.WriteLine(" 12. Cerrar sesión");
                Console.WriteLine(" 0.  Salir");

                if (!string.IsNullOrEmpty(_usuarioActual))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\nSesión activa como: {_usuarioActual}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n No has iniciado sesión.");
                }
                Console.ResetColor();

                Console.Write("\nSelecciona una opción: ");
                string op = Console.ReadLine();

                if (op == "1") await Login();
                else if (op == "2") await RegistrarUsuario();
                else if (op == "3" && !string.IsNullOrEmpty(_usuarioActual)) await PublicarArticulo();
                else if (op == "4" && !string.IsNullOrEmpty(_usuarioActual)) await EditarArticulo();
                else if (op == "5" && !string.IsNullOrEmpty(_usuarioActual)) await RealizarOferta();
                else if (op == "6" && !string.IsNullOrEmpty(_usuarioActual)) await ConsultarArticulo();
                else if (op == "7" && !string.IsNullOrEmpty(_usuarioActual)) await BuscarArticulosPorCategoria();
                else if (op == "8" && !string.IsNullOrEmpty(_usuarioActual)) await DescargarImagenArticulo();
                else if (op == "9" && !string.IsNullOrEmpty(_usuarioActual)) await EliminarArticulo();
                else if (op == "10" && !string.IsNullOrEmpty(_usuarioActual)) await VerHistorialActividades();
                else if (op == "11") await ConsultarDatosLocal();
                else if (op == "12" && !string.IsNullOrEmpty(_usuarioActual)) await CerrarSesion();
                else if (op == "0")
                {
                    Console.WriteLine("Conexión terminada.");
                    break;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Opción inválida o no autenticado.");
                    Console.ResetColor();
                }

                Console.WriteLine("\nPresiona una tecla para volver al menú...");
                Console.ReadKey();
                Console.Clear();
            }
        }


        private async Task Login()
        {
            Console.Write("Usuario: ");
            string usuario = Console.ReadLine();
            Console.Write("Contraseña: ");
            string contrasena = Console.ReadLine();

            await _cliente.EnviarComandoAsync(CommandConstants.Login, $"{usuario}|{contrasena}");
            var (respuesta, cmd) = await _cliente.RecibirRespuestaAsync();

            if (respuesta == "LOGIN_OK")
            {
                _usuarioActual = usuario;
                Console.WriteLine("Login exitoso.");
                try
                {
                    UsuarioLocal usuarioLocal = new UsuarioLocal
                    {
                        NombreUsuarioBytes = EncodeStringToFixedSizeByteArray(usuario, StringByteSize),
                        ClaveBytes = EncodeStringToFixedSizeByteArray(contrasena, StringByteSize)
                    };
                    if (!UsuarioYaExiste(usuario))
                    {
                        GuardarUsuarioLocal(usuarioLocal);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error guardando usuario localmente: {ex.Message}");
                }
            }
            else if (respuesta == "LOGIN_FAIL")
            {
                Console.WriteLine("Usuario o contraseña incorrectos.");
            }
            else
            {
                Console.WriteLine($"Error al iniciar sesión: {respuesta}");
            }
        }

        private async Task RegistrarUsuario()
        {
            Console.Write("Nombre de usuario: ");
            string usuario = Console.ReadLine();
            Console.Write("Contraseña: ");
            string contrasena = Console.ReadLine();

            await _cliente.EnviarComandoAsync(CommandConstants.RegistrarUsuario, $"{usuario}|{contrasena}");
            var (respuesta, cmd) = await _cliente.RecibirRespuestaAsync();

            if (respuesta == "REGISTRO_OK")
            {
                Console.WriteLine("Usuario registrado exitosamente. Ahora puedes iniciar sesión.");
                try
                {
                    UsuarioLocal usuarioLocal = new UsuarioLocal
                    {
                        NombreUsuarioBytes = EncodeStringToFixedSizeByteArray(usuario, StringByteSize),
                        ClaveBytes = EncodeStringToFixedSizeByteArray(contrasena, StringByteSize)
                    };
                    if (!UsuarioYaExiste(usuario))
                    {
                        GuardarUsuarioLocal(usuarioLocal);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error guardando usuario localmente: {ex.Message}");
                }
            }
            else if (respuesta == "USUARIO_YA_EXISTE")
            {
                Console.WriteLine("El usuario ya existe. Por favor, elige otro nombre de usuario.");
            }
            else
            {
                Console.WriteLine($"Error al registrar usuario: {respuesta}");
            }
        }

        private async Task CerrarSesion()
        {
            _usuarioActual = string.Empty;
            Console.WriteLine("Sesión cerrada exitosamente.");
        }

        private async Task PublicarArticulo()
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
            if (!int.TryParse(precio, out _))
            {
                Console.WriteLine("El precio base debe ser un número entero.");
                return;
            }
            if (!DateTime.TryParseExact(fecha, "dd-MM-yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out _))
            {
                Console.WriteLine("Fecha de cierre inválida. Usa el formato dd-MM-yyyy HH:mm.");
                return;
            }

            string datosParaValidar = $"{titulo}|{descripcion}|{categoria}|{precio}|{fecha}|";
            await _cliente.EnviarComandoAsync(CommandConstants.ValidarArticulo, datosParaValidar);
            var (validacion, cmdValidacion) = await _cliente.RecibirRespuestaAsync();

            if (validacion != "VALIDO")
            {
                Console.WriteLine("Error en los datos del artículo. No se enviará la imagen ni se publicará.");
                return;
            }

            Console.Write("¿Deseas agregar una imagen al artículo? (S/N): ");
            string agregarImagen = Console.ReadLine()?.Trim().ToUpper();
            string nombreArchivoImagen = "";
            if (agregarImagen == "S")
            {
                Console.Write("Ruta de la imagen: ");
                string ruta = Console.ReadLine();
                if (File.Exists(ruta))
                {
                    try
                    {
                        nombreArchivoImagen = Path.GetFileName(ruta);
                        await _cliente.EnviarArchivoPorPartesAsync(ruta);
                    }
                    catch
                    {
                        Console.WriteLine("Error al leer o enviar el archivo. Se continuará sin imagen.");
                        nombreArchivoImagen = "";
                    }
                }
                else
                {
                    Console.WriteLine("Archivo no encontrado. Se continuará sin imagen.");
                }
            }

            string datos = $"{titulo}|{descripcion}|{categoria}|{precio}|{fecha}|{nombreArchivoImagen}";
            await _cliente.EnviarComandoAsync(CommandConstants.PublicarArticulo, datos);
            var (respuesta, cmd) = await _cliente.RecibirRespuestaAsync();

            if (!respuesta.Contains("fue publicado correctamente"))
            {
                Console.WriteLine($"Error al publicar: {respuesta}");
            }
            else
            {
                Console.WriteLine($"{respuesta}");
                try
                {
                    int id = int.Parse(respuesta.Split('|')[1].Split('=')[1]);
                    DateTime fechaCierre = DateTime.ParseExact(fecha, "dd-MM-yyyy HH:mm", null);
                    int precioBase = int.Parse(precio);

                    ArticuloLocal articulo = new ArticuloLocal
                    {
                        ID = id,
                        TituloBytes = EncodeStringToFixedSizeByteArray(titulo, StringByteSize),
                        DescripcionBytes = EncodeStringToFixedSizeByteArray(descripcion, StringByteSize),
                        CategoriaBytes = EncodeStringToFixedSizeByteArray(categoria, StringByteSize),
                        PrecioBase = precioBase,
                        FechaCierreTicks = fechaCierre.Ticks,
                        ImagenNombreArchivoBytes = EncodeStringToFixedSizeByteArray(nombreArchivoImagen, StringByteSize),
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
        }

        private async Task EditarArticulo()
        {
            await _cliente.EnviarComandoAsync(CommandConstants.ObtenerArticulosUsuario, "");
            var (respuesta, cmd) = await _cliente.RecibirRespuestaAsync();

            if (respuesta == "SIN_ARTICULOS")
            {
                Console.WriteLine("No tienes artículos para editar.");
                return;
            }

            Console.WriteLine("Tus artículos:");
            Console.WriteLine(respuesta);

            Console.Write("Selecciona el ID del artículo a editar (o 0 para volver): ");
            string seleccion = Console.ReadLine();
            if (seleccion == "0") return;

            if (!int.TryParse(seleccion, out int id))
            {
                Console.WriteLine("ID inválido.");
                return;
            }

            Console.WriteLine("¿Qué deseas editar?");
            Console.WriteLine("1. Título");
            Console.WriteLine("2. Descripción");
            Console.WriteLine("3. Categoría");
            Console.WriteLine("4. Precio base");
            Console.WriteLine("5. Fecha de cierre");
            Console.WriteLine("6. Imagen");
            Console.WriteLine("0. Volver sin editar");

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
                        if (string.IsNullOrWhiteSpace(nuevoTitulo))
                        {
                            Console.WriteLine("El título no puede estar vacío.");
                            nuevoTitulo = "";
                        }
                        break;
                    case "2":
                        Console.Write("Nueva descripción: ");
                        descripcion = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(descripcion))
                        {
                            Console.WriteLine("La descripción no puede estar vacía.");
                            descripcion = "";
                        }
                        break;
                    case "3":
                        Console.Write("Nueva categoría: ");
                        categoria = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(categoria))
                        {
                            Console.WriteLine("La categoría no puede estar vacía.");
                            categoria = "";
                        }
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
                            Console.WriteLine("Fecha de cierre inválida. Usa el formato dd-MM-yyyy HH:mm.");
                            fecha = "";
                        }
                        break;
                    case "6":
                        Console.Write("Ruta de la nueva imagen: ");
                        string ruta = Console.ReadLine();
                        if (File.Exists(ruta))
                        {
                            imagen = Path.GetFileName(ruta);
                            await _cliente.EnviarArchivoPorPartesAsync(ruta);
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

            string datos = $"{seleccion}|{nuevoTitulo}|{descripcion}|{categoria}|{precio}|{fecha}|{imagen}";
            await _cliente.EnviarComandoAsync(CommandConstants.EditarArticulo, datos);
            var (resultado, cmd2) = await _cliente.RecibirRespuestaAsync();
            Console.WriteLine(resultado);

            if (resultado.Contains("editado correctamente"))
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

        private async Task RealizarOferta()
        {
            Console.WriteLine("Lista de artículos en remate:");
            await _cliente.EnviarComandoAsync(CommandConstants.ListarArticulosRemate, "");
            var (respuesta, cmd) = await _cliente.RecibirRespuestaAsync();

            if (string.IsNullOrWhiteSpace(respuesta) || respuesta == "SIN_ARTICULOS")
            {
                Console.WriteLine("No hay artículos activos en remate para ofertar.");
                return;
            }

            Console.WriteLine(respuesta);

            Console.Write("ID del artículo a ofertar: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nID inválido.");
                Console.ResetColor();
                return;
            }
            Console.Write("Monto ofertado: ");
            string monto = Console.ReadLine();
            if (!int.TryParse(monto, out _))
            {
                Console.WriteLine("El monto debe ser un número entero.");
                return;
            }

            await _cliente.EnviarComandoAsync(CommandConstants.RealizarOferta, $"{id}|{monto}");
            var (resultado, cmd2) = await _cliente.RecibirRespuestaAsync();
            if (resultado.Contains("Oferta registrada"))
            {
                try
                {
                    int articuloId = int.Parse(resultado.Split('|')[1].Split('=')[1]);
                    int montoOfertado = int.Parse(monto);
                    OfertaLocal oferta = new OfertaLocal
                    {
                        ArticuloID = articuloId,
                        UsuarioBytes = EncodeStringToFixedSizeByteArray(_usuarioActual, StringByteSize),
                        Monto = montoOfertado,
                        FechaTicks = DateTime.Now.Ticks
                    };
                    if (!OfertaYaExiste(articuloId, montoOfertado, _usuarioActual))
                    {
                        GuardarOfertaLocal(oferta);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error guardando oferta localmente: {ex.Message}");
                }
            }
            Console.WriteLine(resultado);
        }

        private async Task ConsultarArticulo()
        {
            var articulos = LeerTodosArticulosLocales();
            Console.WriteLine($"Artículos locales: {articulos.Count}");

            Console.WriteLine("Lista de artículos:");
            await _cliente.EnviarComandoAsync(CommandConstants.ListarTodosLosArticulos, "");
            var (respuesta, cmd) = await _cliente.RecibirRespuestaAsync();

            if (string.IsNullOrWhiteSpace(respuesta) || respuesta == "SIN_ARTICULOS")
            {
                Console.WriteLine("No hay artículos disponibles.");
                return;
            }

            Console.WriteLine(respuesta);

            Console.Write("ID del artículo a consultar: ");
            string seleccion = Console.ReadLine();
            if (!int.TryParse(seleccion, out _))
            {
                Console.WriteLine("ID inválido.");
                return;
            }

            await _cliente.EnviarComandoAsync(CommandConstants.ConsultarArticulo, seleccion);
            var (detalle, cmd2) = await _cliente.RecibirRespuestaAsync();

            if (detalle.Contains("no encontrado") || detalle == "ID inválido.")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n{detalle}");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("\n=== Detalles del artículo ===");
                Console.WriteLine(detalle);
            }
        }

        private async Task BuscarArticulosPorCategoria()
        {
            Console.Write("Ingrese la categoría para filtrar: ");
            string categoria = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(categoria))
            {
                Console.WriteLine("La categoría no puede estar vacía.");
                return;
            }

            await _cliente.EnviarComandoAsync(CommandConstants.FiltrarArticulosPorCategoria, categoria);
            var (respuesta, cmd2) = await _cliente.RecibirRespuestaAsync();

            if (string.IsNullOrWhiteSpace(respuesta) || respuesta == "SIN_ARTICULOS")
            {
                Console.WriteLine("No hay artículos en esa categoría.");
                return;
            }

            Console.WriteLine("Artículos encontrados:");
            Console.WriteLine(respuesta);
        }

        private async Task DescargarImagenArticulo()
        {
            await _cliente.EnviarComandoAsync(CommandConstants.ListarArticulosConImagen, "");
            var (respuesta, cmd2) = await _cliente.RecibirRespuestaAsync();

            if (respuesta == "SIN_IMAGENES")
            {
                Console.WriteLine("No hay artículos con imágenes disponibles.");
                return;
            }

            Console.WriteLine("Artículos con imagen:");
            Console.WriteLine(respuesta);

            Console.Write("Selecciona el número de la imagen que deseas descargar: ");
            string seleccion = Console.ReadLine();

            await _cliente.EnviarComandoAsync(CommandConstants.SolicitarImagenArticulo, seleccion);

            await _cliente.RecibirArchivoPorPartesAsync();
        }


        private async Task EliminarArticulo()
        {
            await _cliente.EnviarComandoAsync(CommandConstants.ObtenerArticulosUsuario, "");
            var (respuesta, cmd) = await _cliente.RecibirRespuestaAsync();

            if (respuesta == "SIN_ARTICULOS")
            {
                Console.WriteLine("No tienes artículos para eliminar.");
                return;
            }

            Console.WriteLine("Tus artículos:");
            Console.WriteLine(respuesta);

            Console.Write("Selecciona el ID del artículo a eliminar: ");
            string seleccion = Console.ReadLine();
            if (!int.TryParse(seleccion, out int id))
            {
                Console.WriteLine("ID inválido.");
                return;
            }

            await _cliente.EnviarComandoAsync(CommandConstants.EliminarArticulo, seleccion);
            var (resultado, cmd2) = await _cliente.RecibirRespuestaAsync();
            Console.WriteLine(resultado);

            if (resultado.Contains("eliminado correctamente"))
            {
                try
                {
                    EliminarArticuloLocal(id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error eliminando artículo localmente: {ex.Message}");
                }
            }
        }

        private async Task VerHistorialActividades()
        {
            Console.WriteLine("\n=== Historial de Actividades ===");

            Console.WriteLine("\nArtículos publicados:");
            await _cliente.EnviarComandoAsync(CommandConstants.ObtenerArticulosUsuario, "");
            var (respuestaArticulos, cmd) = await _cliente.RecibirRespuestaAsync();
            if (respuestaArticulos == "SIN_ARTICULOS")
            {
                Console.WriteLine("No has publicado artículos.");
            }
            else
            {
                Console.WriteLine(respuestaArticulos);
            }

            Console.WriteLine("\nOfertas realizadas:");
            await _cliente.EnviarComandoAsync(CommandConstants.ObtenerOfertasUsuario, "");
            var (respuestaOfertas, cmd2) = await _cliente.RecibirRespuestaAsync();
            if (respuestaOfertas == "SIN_OFERTAS")
            {
                Console.WriteLine("No has realizado ofertas.");
            }
            else
            {
                Console.WriteLine(respuestaOfertas);
            }

            Console.WriteLine("\nRemates ganados:");
            await _cliente.EnviarComandoAsync(CommandConstants.ObtenerRematesGanados, "");
            var (respuestaRemates, cmd3) = await _cliente.RecibirRespuestaAsync();
            if (respuestaRemates == "SIN_REMATES")
            {
                Console.WriteLine("No has ganado ningún remate.");
            }
            else
            {
                Console.WriteLine(respuestaRemates);
                var lineas = respuestaRemates.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                foreach (var linea in lineas)
                {
                    try
                    {
                        var partes = linea.Split(',');
                        int articuloId = int.Parse(partes[0].Split('=')[1]);
                        string titulo = partes[1].Split('=')[1].Trim();
                        int precioFinal = int.Parse(partes[2].Split('=')[1]);
                        DateTime fechaCierre = DateTime.ParseExact(partes[3].Split('=')[1].Trim(), "dd-MM-yyyy HH:mm", null);

                        RemateGanadoLocal remate = new RemateGanadoLocal
                        {
                            ArticuloID = articuloId,
                            TituloBytes = EncodeStringToFixedSizeByteArray(titulo, StringByteSize),
                            PrecioFinal = precioFinal,
                            UsuarioGanadorBytes = EncodeStringToFixedSizeByteArray(_usuarioActual, StringByteSize),
                            FechaCierreTicks = fechaCierre.Ticks
                        };
                        if (!RemateYaExiste(articuloId))
                        {
                            GuardarRemateGanadoLocal(remate);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error guardando remate localmente: {ex.Message}");
                    }
                }
            }
        }

        private async Task ConsultarDatosLocal()
        {
            Console.WriteLine("\n=== Artículos Locales ===");
            try
            {
                if (!File.Exists(ArticulosFilePath))
                {
                    Console.WriteLine("No hay artículos almacenados localmente.");
                }
                else
                {
                    using (FileStream fs = new FileStream(ArticulosFilePath, FileMode.Open, FileAccess.Read))
                    {
                        int recordSize = sizeof(int) + 5 * StringByteSize + sizeof(int) + sizeof(long) + sizeof(bool);
                        int recordCount = (int)(fs.Length / recordSize);
                        if (recordCount == 0)
                        {
                            Console.WriteLine("No hay artículos almacenados localmente.");
                        }
                        else
                        {
                            Console.WriteLine($"Total de artículos locales: {recordCount}");
                            for (int i = 0; i < recordCount; i++)
                            {
                                LeerArticuloLocal(i);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error consultando artículos locales: {ex.Message}");
            }

            Console.WriteLine("\n=== Ofertas Locales ===");
            try
            {
                if (!File.Exists(OfertasFilePath))
                {
                    Console.WriteLine("No hay ofertas almacenadas localmente.");
                }
                else
                {
                    using (FileStream fs = new FileStream(OfertasFilePath, FileMode.Open, FileAccess.Read))
                    {
                        int recordSize = sizeof(int) + StringByteSize + sizeof(int) + sizeof(long);
                        int recordCount = (int)(fs.Length / recordSize);
                        if (recordCount == 0)
                        {
                            Console.WriteLine("No hay ofertas almacenadas localmente.");
                        }
                        else
                        {
                            Console.WriteLine($"Total de ofertas locales: {recordCount}");
                            for (int i = 0; i < recordCount; i++)
                            {
                                LeerOfertaLocal(i);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error consultando ofertas locales: {ex.Message}");
            }

            Console.WriteLine("\n=== Remates Ganados Locales ===");
            try
            {
                if (!File.Exists(RematesFilePath))
                {
                    Console.WriteLine("No hay remates ganados almacenados localmente.");
                }
                else
                {
                    using (FileStream fs = new FileStream(RematesFilePath, FileMode.Open, FileAccess.Read))
                    {
                        int recordSize = sizeof(int) + 2 * StringByteSize + sizeof(int) + sizeof(long);
                        int recordCount = (int)(fs.Length / recordSize);
                        if (recordCount == 0)
                        {
                            Console.WriteLine("No hay remates ganados almacenados localmente.");
                        }
                        else
                        {
                            Console.WriteLine($"Total de remates ganados locales: {recordCount}");
                            for (int i = 0; i < recordCount; i++)
                            {
                                LeerRemateGanadoLocal(i);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error consultando remates ganados locales: {ex.Message}");
            }

            Console.WriteLine("\n=== Usuarios Locales ===");
            try
            {
                if (!File.Exists(UsuariosFilePath))
                {
                    Console.WriteLine("No hay usuarios almacenados localmente.");
                }
                else
                {
                    using (FileStream fs = new FileStream(UsuariosFilePath, FileMode.Open, FileAccess.Read))
                    {
                        int recordSize = 2 * StringByteSize;
                        int recordCount = (int)(fs.Length / recordSize);
                        if (recordCount == 0)
                        {
                            Console.WriteLine("No hay usuarios almacenados localmente.");
                        }
                        else
                        {
                            Console.WriteLine($"Total de usuarios locales: {recordCount}");
                            for (int i = 0; i < recordCount; i++)
                            {
                                LeerUsuarioLocal(i);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error consultando usuarios locales: {ex.Message}");
            }
        }

        private bool ArticuloYaExiste(int id)
        {
            if (!File.Exists(ArticulosFilePath)) return false;

            using (FileStream fs = new FileStream(ArticulosFilePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int recordSize = sizeof(int) + 4 * StringByteSize + sizeof(int) + sizeof(long) + sizeof(bool);
                int recordCount = (int)(fs.Length / recordSize);
                for (int i = 0; i < recordCount; i++)
                {
                    fs.Seek(i * recordSize, SeekOrigin.Begin);
                    int existingId = reader.ReadInt32();
                    if (existingId == id) return true;
                }
            }
            return false;
        }

        private bool OfertaYaExiste(int articuloId, int monto, string usuario)
        {
            if (!File.Exists(OfertasFilePath)) return false;

            using (FileStream fs = new FileStream(OfertasFilePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int recordSize = sizeof(int) + StringByteSize + sizeof(int) + sizeof(long);
                int recordCount = (int)(fs.Length / recordSize);
                for (int i = 0; i < recordCount; i++)
                {
                    fs.Seek(i * recordSize, SeekOrigin.Begin);
                    int existingArticuloId = reader.ReadInt32();
                    byte[] usuarioBytes = reader.ReadBytes(StringByteSize);
                    int existingMonto = reader.ReadInt32();
                    string existingUsuario = DecodeByteArrayToString(usuarioBytes);
                    if (existingArticuloId == articuloId && existingMonto == monto && existingUsuario == usuario) return true;
                }
            }
            return false;
        }

        private bool RemateYaExiste(int articuloId)
        {
            if (!File.Exists(RematesFilePath)) return false;

            using (FileStream fs = new FileStream(RematesFilePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int recordSize = sizeof(int) + 2 * StringByteSize + sizeof(int) + sizeof(long);
                int recordCount = (int)(fs.Length / recordSize);
                for (int i = 0; i < recordCount; i++)
                {
                    fs.Seek(i * recordSize, SeekOrigin.Begin);
                    int existingArticuloId = reader.ReadInt32();
                    if (existingArticuloId == articuloId) return true;
                }
            }
            return false;
        }

        private bool UsuarioYaExiste(string nombreUsuario)
        {
            if (!File.Exists(UsuariosFilePath)) return false;

            using (FileStream fs = new FileStream(UsuariosFilePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int recordSize = 2 * StringByteSize;
                int recordCount = (int)(fs.Length / recordSize);
                for (int i = 0; i < recordCount; i++)
                {
                    fs.Seek(i * recordSize, SeekOrigin.Begin);
                    byte[] nombreUsuarioBytes = reader.ReadBytes(StringByteSize);
                    string existingNombreUsuario = DecodeByteArrayToString(nombreUsuarioBytes);
                    if (existingNombreUsuario == nombreUsuario) return true;
                }
            }
            return false;
        }

        private List<ArticuloLocal> LeerTodosArticulosLocales()
        {
            List<ArticuloLocal> articulos = new List<ArticuloLocal>();
            if (!File.Exists(ArticulosFilePath)) return articulos;

            using (FileStream fs = new FileStream(ArticulosFilePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int recordSize = sizeof(int) + 4 * StringByteSize + sizeof(int) + sizeof(long) + sizeof(bool);
                int recordCount = (int)(fs.Length / recordSize);
                for (int i = 0; i < recordCount; i++)
                {
                    fs.Seek(i * recordSize, SeekOrigin.Begin);
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

        private void ActualizarArticuloLocal(int id, ArticuloLocal articuloActualizado)
        {
            var articulos = LeerTodosArticulosLocales();
            int index = articulos.FindIndex(a => a.ID == id);
            if (index >= 0)
            {
                articulos[index] = articuloActualizado;
                File.Delete(ArticulosFilePath);
                foreach (var articulo in articulos)
                {
                    GuardarArticuloLocal(articulo);
                }
            }
        }

        private void EliminarArticuloLocal(int id)
        {
            var articulos = LeerTodosArticulosLocales();
            articulos.RemoveAll(a => a.ID == id);
            File.Delete(ArticulosFilePath);
            foreach (var articulo in articulos)
            {
                GuardarArticuloLocal(articulo);
            }
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

        private void LeerArticuloLocal(int numeroRegistro)
        {
            try
            {
                using (FileStream fs = new FileStream(ArticulosFilePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    int recordSize = sizeof(int) + 5 * StringByteSize + sizeof(int) + sizeof(long) + sizeof(bool);
                    fs.Seek(numeroRegistro * recordSize, SeekOrigin.Begin);

                    int id = reader.ReadInt32();
                    byte[] tituloBytes = reader.ReadBytes(StringByteSize);
                    byte[] descripcionBytes = reader.ReadBytes(StringByteSize);
                    byte[] categoriaBytes = reader.ReadBytes(StringByteSize);
                    int precioBase = reader.ReadInt32();
                    long fechaCierreTicks = reader.ReadInt64();
                    byte[] imagenNombreArchivoBytes = reader.ReadBytes(StringByteSize);
                    byte[] usuarioBytes = reader.ReadBytes(StringByteSize);
                    bool finalizado = reader.ReadBoolean();
                    byte[] usuarioGanadorBytes = reader.ReadBytes(StringByteSize);

                    string titulo = DecodeByteArrayToString(tituloBytes);
                    string descripcion = DecodeByteArrayToString(descripcionBytes);
                    string categoria = DecodeByteArrayToString(categoriaBytes);
                    DateTime fechaCierre = new DateTime(fechaCierreTicks);
                    string imagenNombreArchivo = DecodeByteArrayToString(imagenNombreArchivoBytes);
                    string usuario = DecodeByteArrayToString(usuarioBytes);
                    string usuarioGanador = DecodeByteArrayToString(usuarioGanadorBytes);

                    Console.WriteLine($"Artículo {numeroRegistro + 1}: ID={id}, Título={titulo}, Descripción={descripcion}, Categoría={categoria}, PrecioBase={precioBase}, FechaCierre={fechaCierre:dd-MM-yyyy HH:mm}, Imagen={imagenNombreArchivo}, Usuario={usuario}, Finalizado={finalizado}, Ganador={usuarioGanador}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error leyendo artículo local {numeroRegistro + 1}: {ex.Message}");
            }
        }

        private void GuardarOfertaLocal(OfertaLocal oferta)
        {
            try
            {
                using (FileStream fs = new FileStream(OfertasFilePath, FileMode.Append, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    writer.Write(oferta.ArticuloID);
                    writer.Write(oferta.UsuarioBytes);
                    writer.Write(oferta.Monto);
                    writer.Write(oferta.FechaTicks);
                }
                Console.WriteLine("Oferta guardada localmente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando oferta localmente: {ex.Message}");
            }
        }

        private void LeerOfertaLocal(int numeroRegistro)
        {
            try
            {
                using (FileStream fs = new FileStream(OfertasFilePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    int recordSize = sizeof(int) + StringByteSize + sizeof(int) + sizeof(long);
                    fs.Seek(numeroRegistro * recordSize, SeekOrigin.Begin);

                    int articuloId = reader.ReadInt32();
                    byte[] usuarioBytes = reader.ReadBytes(StringByteSize);
                    int monto = reader.ReadInt32();
                    long fechaTicks = reader.ReadInt64();

                    string usuario = DecodeByteArrayToString(usuarioBytes);
                    DateTime fecha = new DateTime(fechaTicks);

                    Console.WriteLine($"Oferta {numeroRegistro + 1}: ArtículoID={articuloId}, Usuario={usuario}, Monto={monto}, Fecha={fecha:dd-MM-yyyy HH:mm}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error leyendo oferta local {numeroRegistro + 1}: {ex.Message}");
            }
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
                Console.WriteLine("Remate ganado guardado localmente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando remate localmente: {ex.Message}");
            }
        }

        private void LeerRemateGanadoLocal(int numeroRegistro)
        {
            try
            {
                using (FileStream fs = new FileStream(RematesFilePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    int recordSize = sizeof(int) + 2 * StringByteSize + sizeof(int) + sizeof(long);
                    fs.Seek(numeroRegistro * recordSize, SeekOrigin.Begin);

                    int articuloId = reader.ReadInt32();
                    byte[] tituloBytes = reader.ReadBytes(StringByteSize);
                    int precioFinal = reader.ReadInt32();
                    byte[] usuarioGanadorBytes = reader.ReadBytes(StringByteSize);
                    long fechaCierreTicks = reader.ReadInt64();

                    string titulo = DecodeByteArrayToString(tituloBytes);
                    string usuarioGanador = DecodeByteArrayToString(usuarioGanadorBytes);
                    DateTime fechaCierre = new DateTime(fechaCierreTicks);

                    Console.WriteLine($"Remate {numeroRegistro + 1}: ArtículoID={articuloId}, Título={titulo}, PrecioFinal={precioFinal}, Ganador={usuarioGanador}, FechaCierre={fechaCierre:dd-MM-yyyy HH:mm}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error leyendo remate local {numeroRegistro + 1}: {ex.Message}");
            }
        }

        private void GuardarUsuarioLocal(UsuarioLocal usuario)
        {
            try
            {
                using (FileStream fs = new FileStream(UsuariosFilePath, FileMode.Append, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    writer.Write(usuario.NombreUsuarioBytes);
                    writer.Write(usuario.ClaveBytes);
                }
                Console.WriteLine("Usuario guardado localmente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando usuario localmente: {ex.Message}");
            }
        }

        private void LeerUsuarioLocal(int numeroRegistro)
        {
            try
            {
                using (FileStream fs = new FileStream(UsuariosFilePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    int recordSize = 2 * StringByteSize;
                    fs.Seek(numeroRegistro * recordSize, SeekOrigin.Begin);

                    byte[] nombreUsuarioBytes = reader.ReadBytes(StringByteSize);
                    byte[] claveBytes = reader.ReadBytes(StringByteSize);

                    string nombreUsuario = DecodeByteArrayToString(nombreUsuarioBytes);
                    string clave = DecodeByteArrayToString(claveBytes);

                    Console.WriteLine($"Usuario {numeroRegistro + 1}: Nombre={nombreUsuario}, Clave={clave}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error leyendo usuario local {numeroRegistro + 1}: {ex.Message}");
            }
        }

        private static byte[] EncodeStringToFixedSizeByteArray(string input, int fixedSize)
        {
            byte[] stringBytes = Encoding.UTF8.GetBytes(input ?? "");
            Array.Resize(ref stringBytes, fixedSize);
            return stringBytes;
        }

        private static string DecodeByteArrayToString(byte[] byteArray)
        {
            int actualLength = Array.IndexOf(byteArray, (byte)0);
            if (actualLength < 0) actualLength = byteArray.Length;
            return Encoding.UTF8.GetString(byteArray, 0, actualLength).TrimEnd('\0');
        }
    }
}