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
        while (true)
        {
            Console.WriteLine("\n1. Login\n0. Salir");
            Console.Write("Opción: ");
            var opcion = Console.ReadLine();

            if (opcion == "1")
            {
                Console.Write("Usuario: ");
                string usuario = Console.ReadLine();

                Console.Write("Clave: ");
                string clave = Console.ReadLine();

                _cliente.Enviar($"LOGIN|{usuario}|{clave}");
                string respuesta = _cliente.Recibir();
                Console.WriteLine($"Servidor respondió: {respuesta}");
            }
            else if (opcion == "0")
            {
                break;
            }
            else
            {
                Console.WriteLine("Opción inválida.");
            }
        }
    }
}
