using Grpc.Core;
using Remate.GRPC;
using System.Text;
using System.Threading.Channels;

namespace Servidor.Servicios;

public class AdministracionServicio : Administracion.AdministracionBase
{
    private readonly ArticuloServicio _articuloServicio = new();

    private static readonly List<Channel<InicioSesionResponse>> _subscriptores = new();

    public static void NotificarInicioSesion(string nombreUsuario)
    {
        if (string.Equals(nombreUsuario, "admin", StringComparison.OrdinalIgnoreCase))
            return; 

        var evento = new InicioSesionResponse
        {
            NombreUsuario = nombreUsuario,
            Timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")
        };

        lock (_subscriptores)
        {
            foreach (var canal in _subscriptores)
                canal.Writer.TryWrite(evento);
        }
    }

    public override Task<ABMArticuloResponse> ABMArticulo(ABMArticuloRequest request, ServerCallContext context)
    {
        string resultado;
        bool exito;
        string operacion = request.Operacion?.Trim().ToLower();

        Console.WriteLine($"[gRPC] Operación recibida: '{operacion}'");

        var articulos = _articuloServicio.RetornaArticulos();

        switch (operacion)
        {
            case "alta":
                string datosAlta = $"{request.Titulo}|{request.Descripcion}|{request.Categoria}|{request.PrecioBase}|{request.FechaCierre}|{request.ImagenNombreArchivo}";
                resultado = _articuloServicio.PublicarArticulo(datosAlta, request.Usuario, out exito);
                break;

            case "baja":
                string datosBaja = $"{request.Id}|{request.FechaCierre}";
                resultado = _articuloServicio.EliminarArticulo(request.Id.ToString(), request.Usuario, out exito);
                break;

            case "modificacion":
                string datosMod = $"{request.Id}|{request.Titulo}|{request.Descripcion}|{request.Categoria}|{request.PrecioBase}|{request.FechaCierre}|{request.ImagenNombreArchivo}";
                resultado = _articuloServicio.EditarArticulo(datosMod, request.Usuario, out exito);
                break;

            case "listar":
                var disponibles = articulos
                                .Where(a =>
                                    !a.Ofertas.Any() &&
                                    a.FechaCierre > DateTime.Now)
                                .Select(a => $"{a.ID}|{a.Titulo}|{a.Descripcion}|{a.Categoria}|{a.PrecioBase}|{a.FechaCierre}")
                                .ToList(); if (disponibles.Count == 0)
                {
                    resultado = "SIN_ARTICULOS";
                }
                else
                {
                    resultado = string.Join("\n", disponibles);
                }

                exito = true;
                break;

            default:
                resultado = "Operación no válida.";
                exito = false;
                break;
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
        var canal = Channel.CreateUnbounded<InicioSesionResponse>();
        lock (_subscriptores)
        {
            _subscriptores.Add(canal);
        }

        int enviados = 0;
        try
        {
            await foreach (var evento in canal.Reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(evento);
                enviados++;
                if (enviados >= request.Cantidad)
                    break;
            }
        }
        finally
        {
            lock (_subscriptores)
            {
                _subscriptores.Remove(canal);
            }
        }
    }


}
