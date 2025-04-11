namespace Cliente;

public class Program
{
    static void Main()
    {
        var cliente = new Cliente();
        cliente.Conectar();

        var menu = new MenuCliente(cliente);
        menu.Mostrar();
    }
}