namespace Common
{
    public static class ProtocoloImagen
    {
        public const int LargoFijo = 4;
        public const int LargoFijoArchivo = 8;
        public const int MaxFileSizePart = 32768;

        public static long CalcularCantidadDePartes(long fileSize)
        {
            return (fileSize % MaxFileSizePart == 0) ? fileSize / MaxFileSizePart : fileSize / MaxFileSizePart + 1;
        }
    }
}
