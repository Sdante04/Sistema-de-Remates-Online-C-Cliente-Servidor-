namespace Cliente;

class Program
{
    static void Main(string[] args)
    {
        Cliente cliente = new Cliente();
        cliente.Conectar("127.0.0.1", 5000);

        MenuCliente menu = new MenuCliente(cliente);
        menu.Mostrar();
    }
}