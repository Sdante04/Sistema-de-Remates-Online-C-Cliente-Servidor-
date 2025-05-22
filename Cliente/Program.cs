namespace Cliente;

public class Program
{
    public static async Task Main()
    {
        var cliente = new Cliente();
        try
        {
            await cliente.ConectarAsync();
            var menu = new MenuCliente(cliente);
            await menu.MostrarAsync();
        }
        finally
        {
            cliente.Cerrar();
        }
    }
}