using System;

namespace Servidor.Utils
{
    public static class Logger
    {
        public static void Log(string mensaje)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {mensaje}");
            Console.ResetColor();
        }

        public static void Error(string mensaje)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR {DateTime.Now:HH:mm:ss}] {mensaje}");
            Console.ResetColor();
        }

        public static void Warn(string mensaje)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[ADVERTENCIA {DateTime.Now:HH:mm:ss}] {mensaje}");
            Console.ResetColor();
        }
    }
}
