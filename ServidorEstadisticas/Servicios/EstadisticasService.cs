using Common.Models;

namespace ServidorEstadisticas.Servicios
{
    public static class EstadisticasService
    {
        public static Dictionary<string, int> ObtenerEstadisticasDeLogins()
        {
            return AlmacenEventos.EventosUsuario
                .GroupBy(e => e.Usuario)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
