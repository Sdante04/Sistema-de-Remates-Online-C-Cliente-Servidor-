namespace Cliente;

public class Program
{
   public static void Main()
    {
        var cliente = new Cliente();
        cliente.Conectar();
        cliente.IniciarEscuchaServidor();

        var menu = new MenuCliente(cliente);
        menu.Mostrar();
    }
}