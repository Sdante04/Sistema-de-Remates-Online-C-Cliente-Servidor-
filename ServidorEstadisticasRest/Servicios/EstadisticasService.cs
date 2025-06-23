using Common.Models;

namespace ServidorEstadisticasRest.Servicios
{
    public static class EstadisticasService
    {
        public static Dictionary<string, int> ObtenerEstadisticasDeLogins()
        {
            return AlmacenEventos.EventosUsuario
                .GroupBy(e => e.Usuario)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public static async Task<Dictionary<string, int>> ObtenerEventosPorUsuario(string nombreUsuario)
        {
            var eventos = AlmacenEventos.TodosLosEventos
                .Where(e => e is EventoArticulo or EventoOferta or EventoRemate)
                .ToList();

            Dictionary<string, int> eventosPorUsuario = new();
            foreach (var evento in eventos)
            {
                await Task.Delay(5000);
                string tipo = evento.Tipo;

                string? usuario = evento switch
                {
                    EventoArticulo ea => ea.Usuario,
                    EventoOferta eo => eo.Usuario,
                    EventoRemate er => er.UsuarioGanador,
                    _ => null
                };

                if (usuario?.Trim() == nombreUsuario.Trim())
                {
                    if (eventosPorUsuario.ContainsKey(tipo))
                        eventosPorUsuario[tipo]++;
                    else
                        eventosPorUsuario[tipo] = 1;
                }
            }

            return eventosPorUsuario;
        }
    }
}
