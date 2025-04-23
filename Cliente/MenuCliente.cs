using System;
using Common.Common;

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
                Console.WriteLine("\n1. Login");
                Console.WriteLine("2. Publicar artículo");
                Console.WriteLine("3. Editar artículo");
                Console.WriteLine("0. Salir");
                Console.Write("Opción: ");
                string op = Console.ReadLine();

                if (op == "1") Login();
                else if (op == "2" && !string.IsNullOrEmpty(_usuarioActual)) PublicarArticulo();
                else if (op == "3" && !string.IsNullOrEmpty(_usuarioActual)) EditarArticulo();
                else if (op == "0") { Console.WriteLine("Conexión terminada."); break; }
                else Console.WriteLine("Opción inválida o no autenticado.");
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


    }
}


