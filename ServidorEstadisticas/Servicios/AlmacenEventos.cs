using System.Collections.Concurrent;
using Common.Models;

namespace ServidorEstadisticas.Servicios
{
    public static class AlmacenEventos
    {
        public static List<EventoBase> TodosLosEventos { get; } = new();

        public static List<EventoArticulo> EventosArticulo { get; } = new();

        public static List<EventoOferta> EventosOferta { get; } = new();

        public static List<EventoRemate> EventosRemate { get; } = new();

        public static List<EventoUsuario> EventosUsuario { get; } = new();

        private static readonly object _lock = new();

        public static void AgregarEvento(EventoBase evento)
        {
            lock (_lock)
            {
                TodosLosEventos.Add(evento);

                switch (evento)
                {
                    case EventoArticulo art:
                        EventosArticulo.Add(art);
                        break;
                    case EventoOferta oferta:
                        EventosOferta.Add(oferta);
                        break;
                    case EventoRemate remate:
                        EventosRemate.Add(remate);
                        break;
                    case EventoUsuario login:
                        EventosUsuario.Add(login);
                        break;
                }
            }
        }
    }
}
