using System;

namespace Common.Models
{
    public class EventoOferta : EventoBase
    {
        public int ArticuloId { get; set; }
        public string Usuario { get; set; }
        public int Monto { get; set; }
    }
}
