using System;

namespace Common.Models
{
    public class EventoArticulo : EventoBase
    {
        public int ArticuloId { get; set; }
        public string Titulo { get; set; }
        public int PrecioBase { get; set; }
        public string Usuario { get; set; }
    }
}
