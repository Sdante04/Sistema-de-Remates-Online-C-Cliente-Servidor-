using Common.Models;

namespace ServidorEstadisticas.Servicios
{
    public static class FiltroEventosService
    {
        public static List<EventoBase> FiltrarEventos(string? usuario = null, int? articuloId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            var eventosFiltrables = AlmacenEventos.TodosLosEventos
                .Where(e => e is EventoArticulo || e is EventoOferta || e is EventoRemate)
                .ToList();

            return eventosFiltrables.Where(e =>
            {
                bool cumple = true;

                if (!string.IsNullOrWhiteSpace(usuario))
                {
                    if (e is EventoArticulo ea && ea.Usuario != usuario) cumple = false;
                    else if (e is EventoOferta eo && eo.Usuario != usuario) cumple = false;
                    else if (e is EventoRemate er && er.UsuarioGanador != usuario) cumple = false;
                }

                if (articuloId.HasValue)
                {
                    if (e is EventoArticulo ea && ea.ArticuloId != articuloId) cumple = false;
                    else if (e is EventoOferta eo && eo.ArticuloId != articuloId) cumple = false;
                    else if (e is EventoRemate er && er.ArticuloId != articuloId) cumple = false;
                }

                if (desde.HasValue && e.Fecha < desde.Value)
                    cumple = false;

                if (hasta.HasValue && e.Fecha > hasta.Value)
                    cumple = false;

                return cumple;
            }).ToList();
        }
    }
}
