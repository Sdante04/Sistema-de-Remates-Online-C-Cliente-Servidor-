using Microsoft.AspNetCore.Mvc;
using ServidorEstadisticasRest.Servicios;

namespace ServidorEstadisticasRest.Controllers
{
    [ApiController]
    [Route("estadisticas")]
    public class EstadisticasController : ControllerBase
    {
        [HttpGet("login")]
        public IActionResult EstadisticasLogin()
        {
            var estadisticas = EstadisticasService.ObtenerEstadisticasDeLogins();
            return Ok(new { estadisticas });
        }

        [HttpGet("eventos")]
        public IActionResult EventosFiltrados([FromQuery] string? usuario, [FromQuery] int? articuloId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var eventos = FiltroEventosService.FiltrarEventos(usuario, articuloId, desde, hasta);
            return Ok(new { eventos });
        }
    }
}
