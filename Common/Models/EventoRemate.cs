using System;

namespace Common.Models
{
    public class EventoRemate : EventoBase
    {
        public int ArticuloId { get; set; }
        public string UsuarioGanador { get; set; }
        public int MontoFinal { get; set; }
    }
}
