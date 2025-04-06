using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cliente;

public class MenuCliente
{
    private Cliente _cliente;

    public MenuCliente(Cliente cliente)
    {
        _cliente = cliente;
    }

    public void Mostrar()
    {
        bool salir = false;
        while (!salir)
        {
            Console.WriteLine("\n==== MENÚ ====");
            Console.WriteLine("1. Iniciar sesión");
            Console.WriteLine("2. Salir");
            Console.Write("Elija una opción: ");
            string opcion = Console.ReadLine();

            switch (opcion)
            {
                case "1":
                    Login();
                    break;
                case "2":
                    salir = true;
                    break;
                default:
                    Console.WriteLine("Opción inválida.");
                    break;
            }
        }
    }

    private void Login()
    {
        Console.Write("Usuario: ");
        string usuario = Console.ReadLine();

        Console.Write("Clave: ");
        string clave = Console.ReadLine();

        string mensaje = $"LOGIN|{usuario}|{clave}";
        _cliente.Enviar(mensaje);

        string respuesta = _cliente.Recibir();
        Console.WriteLine($"Servidor respondió: {respuesta}");
    }
}
