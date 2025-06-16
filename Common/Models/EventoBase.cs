using System;

namespace Common.Models
{
    public abstract class EventoBase
    {
        public string Tipo { get; set; }    
        public DateTime Fecha { get; set; }    
    }
}
