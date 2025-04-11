namespace Cliente;

public class Program
{
   public static void Main()
    {
        var cliente = new Cliente();
        cliente.Conectar();

        var menu = new MenuCliente(cliente);
        menu.Mostrar();
    }
}