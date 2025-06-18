using Microsoft.AspNetCore.Mvc;
using ServidorEstadisticasRest.Servicios;

namespace ServidorEstadisticasRest.Controllers
{
    [ApiController]
    [Route("reporte")]
    public class ReporteController : ControllerBase
    {
        [HttpPost("crear")]
        public IActionResult Crear(string usuario, string? webhook = null)
        {
            var id = ReportesManager.GenerarReporte(usuario, webhook);
            return Ok(new { id });
        }

        [HttpGet("estado/{id}")]
        public IActionResult Estado(Guid id)
        {
            var estado = ReportesManager.ObtenerEstado(id);
            return Ok(new { estado });
        }

        [HttpGet("resultado/{id}")]
        public IActionResult Resultado(Guid id)
        {
            var resultado = ReportesManager.ObtenerResultado(id);
            if (resultado is null)
                return NotFound();

            var partes = resultado
             .Select(kvp => $"{kvp.Key}: {kvp.Value}")
             .ToList();

            return Ok(new { partes });

        }
    }
}
