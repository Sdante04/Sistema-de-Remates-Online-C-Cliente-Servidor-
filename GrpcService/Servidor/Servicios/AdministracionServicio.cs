using Grpc.Core;
using Remate.GRPC;
using System.Text;

namespace Servidor.Servicios;

public class AdministracionServicio : Administracion.AdministracionBase
{
    private readonly ArticuloServicio _articuloServicio = new();

    public override Task<ABMArticuloResponse> ABMArticulo(ABMArticuloRequest request, ServerCallContext context)
    {
        string resultado;
        bool exito;

        if (request.Operacion == "alta")
        {
            string datos = $"{request.Titulo}|{request.Descripcion}|{request.Categoria}|{request.PrecioBase}|{DateTime.Now.AddDays(7):dd-MM-yyyy HH:mm}|imagen.png";
            resultado = _articuloServicio.PublicarArticulo(datos, request.Usuario, out exito);
        }
        else if (request.Operacion == "baja")
        {
            resultado = _articuloServicio.EliminarArticulo(request.Id.ToString(), request.Usuario, out exito);
        }
        else if (request.Operacion == "modificacion")
        {
            string datos = $"{request.Id}|{request.Titulo}|{request.Descripcion}|{request.Categoria}|{request.PrecioBase}|{DateTime.Now.AddDays(7):dd-MM-yyyy HH:mm}|imagen.png";
            resultado = _articuloServicio.EditarArticulo(datos, request.Usuario, out exito);
        }
        else
        {
            resultado = "Operación no válida.";
        }

        return Task.FromResult(new ABMArticuloResponse { Mensaje = resultado });
    }


    public override Task<HistorialResponse> ConsultarHistorial(HistorialRequest request, ServerCallContext context)
    {
        string usuario = request.NombreUsuario?.Trim();

        if (string.IsNullOrWhiteSpace(usuario))
        {
            return Task.FromResult(new HistorialResponse
            {
                Actividades = { "ERROR: Debes ingresar un nombre de usuario válido." }
            });
        }

        var actividades = new List<string>();

        var articulosTexto = _articuloServicio.ObtenerArticulosDeUsuario(usuario);

        if (articulosTexto == "SIN_ARTICULOS")
        {
            actividades.Add($"ERROR: El usuario '{usuario}' no existe o no tiene publicaciones.");
            return Task.FromResult(new HistorialResponse { Actividades = { actividades } });
        }

        var lineasArticulos = articulosTexto.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var linea in lineasArticulos)
        {
            actividades.Add($"PUBLICADO | {linea.Trim()}");
        }

        var ofertasTexto = _articuloServicio.ObtenerOfertasDeUsuario(usuario);

        if (!string.IsNullOrWhiteSpace(ofertasTexto))
        {
            var lineasOfertas = ofertasTexto.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var linea in lineasOfertas)
            {
                actividades.Add($"OFERTA | {linea.Trim()}");
            }
        }

        var rematesTexto = _articuloServicio.ObtenerRematesGanadosPorUsuario(usuario);

        if (!string.IsNullOrWhiteSpace(rematesTexto))
        {
            var lineasRemates = rematesTexto.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var linea in lineasRemates)
            {
                actividades.Add($"REMATE_GANADO | {linea.Trim()}");
            }
        }

        return Task.FromResult(new HistorialResponse
        {
            Actividades = { actividades }
        });
    }



    public override async Task VerProximosIniciosSesion(IniciosSesionRequest request, IServerStreamWriter<InicioSesionResponse> responseStream, ServerCallContext context)
    {
        const string path = "iniciosesion.bin";
        const int maxStr = 100;
        const int stringByteSize = maxStr * 4;

        if (!File.Exists(path))
            return;

        var registros = new List<(string usuario, long ticks)>();

        using var fs = File.OpenRead(path);
        using var reader = new BinaryReader(fs);
        while (fs.Position < fs.Length)
        {
            var nameBytes = reader.ReadBytes(stringByteSize);
            long ticks = reader.ReadInt64();
            string user = Encoding.UTF8.GetString(nameBytes).Trim('\0').Trim();
            if (!string.Equals(user, "admin", StringComparison.OrdinalIgnoreCase))
                registros.Add((user, ticks));
        }

        var ultimos = registros
            .OrderByDescending(r => r.ticks)
            .Take(request.Cantidad);

        foreach (var (usuario, ticks) in ultimos)
        {
            var fecha = new DateTime(ticks, DateTimeKind.Utc).ToString("dd-MM-yyyy HH:mm:ss");
            await responseStream.WriteAsync(new InicioSesionResponse
            {
                NombreUsuario = usuario,
                Timestamp = fecha
            });
        }
    }

}
