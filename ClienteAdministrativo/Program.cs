namespace ClienteAdministrativo;

public class Program
{
    public static async Task Main()
    {
        var clienteAdministrativo = new ClienteAdministrativo();
        try
        {
            await clienteAdministrativo.ConectarAsync();
            var menu = new MenuClienteAdministrativo(clienteAdministrativo.ClienteGrpc);
            await menu.IniciarAsync();
        }
        finally
        {
            clienteAdministrativo.Cerrar();
        }
    }
}

