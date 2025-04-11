using Servidor.Dominio;

namespace Servidor.Servicios
{
    public class UsuarioServicio
    {
        public Usuario? Autenticar(string nombreUsuario, string clave)
        {
            if (nombreUsuario == "admin" && clave == "123") // Provisorio
                return new Usuario { NombreUsuario = "admin", Clave = "123" };

            return null;
        }
    }
}
