using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor.Utils
{
    public static class Config
    {
        public const int PUERTO = 5000;

        // Si más adelante queremos usar IP específica:
        public const string IP_SERVIDOR = "127.0.0.1";

        // Podemos agregar mas si hace falta:
        public const int MAX_CLIENTES = 10;
    }
}

