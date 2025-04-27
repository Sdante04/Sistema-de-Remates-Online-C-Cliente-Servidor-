namespace Cliente
{
    public class Program
    {
        public static void Main()
        {
            var cliente = new Cliente();
            try
            {
                cliente.Conectar();
                cliente.IniciarMonitoreoCierre();
                var menu = new MenuCliente(cliente);
                menu.Mostrar();
            }
            finally
            {
                cliente.Cerrar();
            }
        }
    }
}