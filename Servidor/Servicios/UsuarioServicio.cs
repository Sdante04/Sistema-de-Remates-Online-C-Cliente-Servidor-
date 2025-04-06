using Servidor.Dominio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor.Servicios
{
    public class UsuarioServicio
    {
        public Usuario? Autenticar(string nombreUsuario, string clave)
        {
            //hardcodeado por ahora
            if (nombreUsuario == "admin" && clave == "123")
                return new Usuario { NombreUsuario = "admin", Clave = "123" };

            return null;
        }

    }
}
