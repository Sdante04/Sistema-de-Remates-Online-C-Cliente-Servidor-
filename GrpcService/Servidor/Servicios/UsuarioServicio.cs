using Servidor.Dominio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Common.Models;

namespace Servidor.Servicios
{
    public class UsuarioServicio
    {
        private static readonly List<Usuario> _usuarios = new List<Usuario>();
        private const string UsuariosFilePath = "usuarios_servidor.bin";
        private const int MaxStringLength = 100;
        private const int StringByteSize = MaxStringLength * 4;

        public UsuarioServicio()
        {
            CargarUsuariosDesdeArchivo();
            if (!_usuarios.Any())
            {
                _usuarios.Add(new Usuario { NombreUsuario = "admin", Clave = "123" });
                GuardarUsuariosEnArchivo();
            }
        }

        private void CargarUsuariosDesdeArchivo()
        {
            if (!File.Exists(UsuariosFilePath)) return;

            try
            {
                using (FileStream fs = new FileStream(UsuariosFilePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    int recordSize = 2 * StringByteSize;
                    int recordCount = (int)(fs.Length / recordSize);
                    for (int i = 0; i < recordCount; i++)
                    {
                        fs.Seek(i * recordSize, SeekOrigin.Begin);
                        byte[] nombreBytes = reader.ReadBytes(StringByteSize);
                        byte[] claveBytes = reader.ReadBytes(StringByteSize);

                        string nombreUsuario = DecodeByteArrayToString(nombreBytes);
                        string clave = DecodeByteArrayToString(claveBytes);

                        _usuarios.Add(new Usuario { NombreUsuario = nombreUsuario, Clave = clave });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando usuarios desde archivo: {ex.Message}");
            }
        }

        private void GuardarUsuariosEnArchivo()
        {
            try
            {
                File.Delete(UsuariosFilePath);
                using (FileStream fs = new FileStream(UsuariosFilePath, FileMode.Create, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    foreach (var usuario in _usuarios)
                    {
                        writer.Write(EncodeStringToFixedSizeByteArray(usuario.NombreUsuario, StringByteSize));
                        writer.Write(EncodeStringToFixedSizeByteArray(usuario.Clave, StringByteSize));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando usuarios en archivo: {ex.Message}");
            }
        }

        public Usuario? Autenticar(string nombreUsuario, string clave)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(clave))
                return null;

            var usuario = _usuarios.FirstOrDefault(u => u.NombreUsuario == nombreUsuario && u.Clave == clave);

            if (usuario != null)
            {
                _ = EventPublisher.PublicarEventoAsync(new EventoUsuario
                {
                    Tipo = "Login",
                    Fecha = DateTime.Now,
                    Usuario = usuario.NombreUsuario
                });
            }


            return usuario;
        }


        public bool RegistrarUsuario(string nombreUsuario, string clave)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(clave))
                return false;

            if (_usuarios.Any(u => u.NombreUsuario == nombreUsuario))
                return false;

            var nuevoUsuario = new Usuario
            {
                NombreUsuario = nombreUsuario,
                Clave = clave
            };

            _usuarios.Add(nuevoUsuario);
            GuardarUsuariosEnArchivo();
            return true;
        }

        public Usuario? ObtenerUsuarioPorNombre(string nombreUsuario)
        {
            return _usuarios.FirstOrDefault(u => u.NombreUsuario == nombreUsuario);
        }

        public List<Usuario> ObtenerTodosLosUsuarios()
        {
            return _usuarios.ToList();
        }

        private static byte[] EncodeStringToFixedSizeByteArray(string input, int fixedSize)
        {
            byte[] stringBytes = Encoding.UTF8.GetBytes(input ?? "");
            Array.Resize(ref stringBytes, fixedSize);
            return stringBytes;
        }

        private static string DecodeByteArrayToString(byte[] byteArray)
        {
            int actualLength = Array.IndexOf(byteArray, (byte)0);
            if (actualLength < 0) actualLength = byteArray.Length;
            return Encoding.UTF8.GetString(byteArray, 0, actualLength).TrimEnd('\0');
        }
    }
}