using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class EstadoReporte
    {
        public Guid Id { get; set; }
        public string Usuario { get; set; }
        public string Estado { get; set; } = "pendiente";
        public Dictionary<string, int>? Resultado { get; set; }
        public string? Webhook { get; set; }
    }
}
