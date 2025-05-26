using Servidor.Servicios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor.Utils
{
    public class CargadorInicial
    {
        private readonly ArticuloServicio _articuloServicio;
        private readonly UsuarioServicio _usuarioServicio;
        private readonly string _rutaDatos;

        public CargadorInicial(ArticuloServicio articuloServicio, UsuarioServicio usuarioServicio)
        {
            _articuloServicio = articuloServicio;
            _usuarioServicio = usuarioServicio;
            _rutaDatos = Environment.GetEnvironmentVariable("SERVER_DATA_PATH")
                         ?? "/app/Datos-Percargados";
        }

        public async Task CargarTodoAsync()
        {
            if (!Directory.Exists(_rutaDatos))
            {
                Console.WriteLine($"[Init] Carpeta de datos precargados no existe: {_rutaDatos}");
            }
            else
            {
                var archivos = new[] { "articulos.bin", "ofertas.bin", "remates.bin", "usuarios.bin" };
                var copias = new Task[archivos.Length];
                for (int i = 0; i < archivos.Length; i++)
                {
                    string nombre = archivos[i];
                    copias[i] = Task.Run(() =>
                    {
                        var origen = Path.Combine(_rutaDatos, nombre);
                        if (File.Exists(origen))
                        {
                            File.Copy(origen, nombre, overwrite: true);
                            Console.WriteLine($"[Init] Copiado {nombre}");
                        }
                    });
                }
                await Task.WhenAll(copias);
            }

            var tArt = Task.Run(() => _articuloServicio.RecargarDesdeArchivos());
            var tUsr = Task.Run(() => _usuarioServicio.RecargarDesdeArchivo());
            await Task.WhenAll(tArt, tUsr);

            Console.WriteLine("[Init] Carga inicial completada.");
        }
    }
}

