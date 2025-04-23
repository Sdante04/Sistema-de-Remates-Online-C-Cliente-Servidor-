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
                Console.WriteLine("0. Salir");
                Console.Write("Opción: ");
                string op = Console.ReadLine();

                if (op == "1") Login();
                else if (op == "2" && !string.IsNullOrEmpty(_usuarioActual)) PublicarArticulo();
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

            // Validar antes de enviar imagen
            string datosParaValidar = $"{titulo}|{descripcion}|{categoria}|{precio}|{fecha}|";
            _cliente.EnviarComando(CommandConstants.ValidarArticulo, datosParaValidar);
            int cmdValidacion;
            string validacion = _cliente.RecibirRespuesta(out cmdValidacion);

            if (validacion != "VALIDO")
            {
                Console.WriteLine("Error en los datos del artículo. No se enviará la imagen ni se publicará.");
                return;
            }

            // Si es válido, pedir y enviar imagen
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

            // Publicar artículo
            string datos = $"{titulo}|{descripcion}|{categoria}|{precio}|{fecha}|{nombreArchivoImagen}";
            _cliente.EnviarComando(CommandConstants.PublicarArticulo, datos);
            int cmd;
            string respuesta = _cliente.RecibirRespuesta(out cmd);

            if (!respuesta.Contains("fue publicado correctamente"))
                Console.WriteLine($"Error al publicar: {respuesta}");
            else
                Console.WriteLine($"{respuesta}");
        }

    }
}


