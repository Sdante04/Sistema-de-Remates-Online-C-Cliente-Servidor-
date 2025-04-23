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

            Console.Write("¿Deseas agregar una imagen al artículo? (S/N): ");
            string agregarImagen = Console.ReadLine()?.Trim().ToUpper();
            string imagenBase64 = "";

            if (agregarImagen == "S")
            {
                Console.Write("Ruta de la imagen: ");
                string ruta = Console.ReadLine();
                if (File.Exists(ruta))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(ruta);
                        imagenBase64 = Convert.ToBase64String(bytes);
                    }
                    catch
                    {
                        Console.WriteLine("Error al leer el archivo. No se agregará imagen.");
                    }
                }
                else
                {
                    Console.WriteLine("Archivo no encontrado, no se agregará imagen.");
                }
            }

            string datos = $"{titulo}|{descripcion}|{categoria}|{precio}|{fecha}|{imagenBase64}";
            _cliente.EnviarComando(CommandConstants.PublicarArticulo, datos);
            int cmd;
            string respuesta = _cliente.RecibirRespuesta(out cmd);

            if (!respuesta.Contains("fue publicado correctamente"))
                Console.WriteLine("Error al publicar.");
            else
            Console.WriteLine($"Servidor respondió: {respuesta}");
        }

    }
}


