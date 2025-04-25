using Common;

namespace Cliente
{
    public class MenuCliente
    {
        private Cliente _cliente;
        private string _usuarioActual = string.Empty;

        public MenuCliente(Cliente cliente)
        {
            _cliente = cliente;
        }

        public void Mostrar()
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine(" ═════════════════════════════");
                Console.WriteLine("║  MENÚ PRINCIPAL DE CLIENTE  ║");
                Console.WriteLine(" ═════════════════════════════");
                Console.ResetColor();

                Console.WriteLine(" 1.  Login");
                Console.WriteLine(" 2.  Publicar artículo");
                Console.WriteLine(" 3.  Editar artículo");
                Console.WriteLine(" 4.  Realizar oferta");
                Console.WriteLine(" 5.  Consultar artículo");
                Console.WriteLine(" 6.  Descargar imagen de artículo");
                Console.WriteLine(" 7.  Eliminar artículo");
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

                if (op == "1") Login();
                else if (op == "2" && !string.IsNullOrEmpty(_usuarioActual)) PublicarArticulo();
                else if (op == "3" && !string.IsNullOrEmpty(_usuarioActual)) EditarArticulo();
                else if (op == "4" && !string.IsNullOrEmpty(_usuarioActual)) RealizarOferta();
                else if (op == "5" && !string.IsNullOrEmpty(_usuarioActual)) ConsultarArticulo();
                else if (op == "6") DescargarImagenArticulo();
                else if (op == "7" && !string.IsNullOrEmpty(_usuarioActual)) EliminarArticulo();
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

        private void Login()
        {
            Console.Write("Usuario: "); string usuario = Console.ReadLine();
            Console.Write("Clave: "); string contrasena = Console.ReadLine();
            _cliente.EnviarComando(CommandConstants.Login, $"{usuario}|{contrasena}");
            int cmd;
            string respuesta = _cliente.RecibirRespuesta(out cmd);
            if (respuesta == "LOGIN_OK") { _usuarioActual = usuario; Console.WriteLine("Login exitoso."); }
            else Console.WriteLine("Login fallido.");
        }

        private void PublicarArticulo()
        {
            Console.Write("Título: "); string titulo = Console.ReadLine();
            Console.Write("Descripción: "); string descripcion = Console.ReadLine();
            Console.Write("Categoría: "); string categoria = Console.ReadLine();
            Console.Write("Precio base: "); string precio = Console.ReadLine();
            Console.Write("Fecha cierre (dd-MM-yyyy HH:mm): "); string fecha = Console.ReadLine();

            string datosParaValidar = $"{titulo}|{descripcion}|{categoria}|{precio}|{fecha}|";
            _cliente.EnviarComando(CommandConstants.ValidarArticulo, datosParaValidar);
            int cmdValidacion;
            string validacion = _cliente.RecibirRespuesta(out cmdValidacion);

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
                        _cliente.EnviarArchivoPorPartes(ruta);
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
            _cliente.EnviarComando(CommandConstants.PublicarArticulo, datos);
            int cmd;
            string respuesta = _cliente.RecibirRespuesta(out cmd);

            if (!respuesta.Contains("fue publicado correctamente"))
                Console.WriteLine($"Error al publicar: {respuesta}");
            else
                Console.WriteLine($"{respuesta}");
        }

        private void EditarArticulo()
        {
            _cliente.EnviarComando(CommandConstants.ObtenerArticulosUsuario, "");
            int cmd;
            string respuesta = _cliente.RecibirRespuesta(out cmd);

            if (respuesta == "SIN_ARTICULOS")
            {
                Console.WriteLine("No tienes artículos para editar.");
                return;
            }

            Console.WriteLine("Tus artículos:");
            Console.WriteLine(respuesta);

            Console.Write("Selecciona el número del artículo a editar (o 0 para volver): ");
            string seleccion = Console.ReadLine();
            if (seleccion == "0") return;

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
                        break;
                    case "5":
                        Console.Write("Nueva fecha cierre (dd-MM-yyyy HH:mm): ");
                        fecha = Console.ReadLine();
                        break;
                    case "6":
                        Console.Write("Ruta de la nueva imagen: ");
                        string ruta = Console.ReadLine();
                        if (File.Exists(ruta))
                        {
                            imagen = Path.GetFileName(ruta);
                            _cliente.EnviarArchivoPorPartes(ruta);
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
            _cliente.EnviarComando(CommandConstants.EditarArticulo, datos);
            string resultado = _cliente.RecibirRespuesta(out cmd);
            Console.WriteLine(resultado);
        }

        private void RealizarOferta()
        {
            Console.WriteLine("Lista de artículos en remate:");
            _cliente.EnviarComando(CommandConstants.ListarArticulosRemate, "");
            int cmd;
            string respuesta = _cliente.RecibirRespuesta(out cmd);

            if (string.IsNullOrWhiteSpace(respuesta) || respuesta == "SIN_ARTICULOS")
            {
                Console.WriteLine("No hay artículos activos en remate para ofertar.");
                return;
            }

            Console.WriteLine(respuesta);

            var lineas = respuesta.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            int cantidad = lineas.Length;

            Console.Write("Número del artículo a ofertar: ");

            if (!int.TryParse(Console.ReadLine(), out int indice) || indice < 1 || indice > cantidad)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nÍndice inválido.");
                Console.ResetColor();
                return;
            }
            Console.Write("Monto ofertado: ");
            string monto = Console.ReadLine();

            _cliente.EnviarComando(CommandConstants.RealizarOferta, $"{indice}|{monto}");
            string resultado = _cliente.RecibirRespuesta(out cmd);
            Console.WriteLine(resultado);
        }

        private void ConsultarArticulo()
        {
            Console.WriteLine("Lista de artículos:");
            _cliente.EnviarComando(CommandConstants.ListarTodosLosArticulos, "");
            int cmd;
            string respuesta = _cliente.RecibirRespuesta(out cmd);

            var lineas = respuesta.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            if (string.IsNullOrWhiteSpace(respuesta) || respuesta == "SIN_ARTICULOS")
            {
                Console.WriteLine("No hay artículos disponibles.");
                return;
            }

            Console.WriteLine(respuesta);

            Console.Write("Número del artículo a consultar: ");
            string seleccion = Console.ReadLine();
            _cliente.EnviarComando(CommandConstants.ConsultarArticulo, seleccion);
            string detalle = _cliente.RecibirRespuesta(out cmd);

            if (detalle == "Índice fuera de rango." || detalle == "Índice inválido.")
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

        private void DescargarImagenArticulo()
        {
            _cliente.EnviarComando(CommandConstants.ListarArticulosConImagen, "");
            int cmd;
            string respuesta = _cliente.RecibirRespuesta(out cmd);

            if (respuesta == "SIN_IMAGENES")
            {
                Console.WriteLine("No hay artículos con imágenes disponibles.");
                return;
            }

            Console.WriteLine("Artículos con imagen:");
            Console.WriteLine(respuesta);

            Console.Write("Selecciona el número de la imagen que deseas descargar: ");
            string seleccion = Console.ReadLine();

            _cliente.EnviarComando(CommandConstants.SolicitarImagenArticulo, seleccion);

            _cliente.RecibirArchivoPorPartes();
        }

        private void EliminarArticulo()
        {
            _cliente.EnviarComando(CommandConstants.ObtenerArticulosUsuario, "");
            int cmd;
            string respuesta = _cliente.RecibirRespuesta(out cmd);

            if (respuesta == "SIN_ARTICULOS")
            {
                Console.WriteLine("No tienes artículos para eliminar.");
                return;
            }

            Console.WriteLine("Tus artículos:");
            Console.WriteLine(respuesta);

            Console.Write("Selecciona el número del artículo a eliminar: ");
            string seleccion = Console.ReadLine();

            _cliente.EnviarComando(CommandConstants.EliminarArticulo, seleccion);
            string resultado = _cliente.RecibirRespuesta(out cmd);
            Console.WriteLine(resultado);
        }

    }
}