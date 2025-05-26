using System.IO;
using System.Threading.Tasks;

namespace Servidor.Servicios
{
    public class CargadorInicial
    {
        private readonly ArticuloServicio _articuloServicio;
        private readonly UsuarioServicio _usuarioServicio;

        public CargadorInicial(ArticuloServicio articuloServicio, UsuarioServicio usuarioServicio)
        {
            _articuloServicio = articuloServicio;
            _usuarioServicio = usuarioServicio;
        }

        public async Task CargarTodoAsync()
        {
            var tareaArticulos = Task.Run(() => _articuloServicio.RecargarDesdeArchivos());
            var tareaUsuarios = Task.Run(() => _usuarioServicio.RecargarDesdeArchivo());

            await Task.WhenAll(tareaArticulos, tareaUsuarios);
        }
    }
}
