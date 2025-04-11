namespace Common
{
    public class FileStreamHelper
    {
        public byte[] Read(string path, long offset, int length)
        {
            byte[] data = new byte[length];
            using var fs = new FileStream(path, FileMode.Open);
            fs.Position = offset;
            int bytesRead = 0;
            while (bytesRead < length)
            {
                int read = fs.Read(data, bytesRead, length - bytesRead);
                if (read == 0) throw new Exception("No se pudo leer el archivo");
                bytesRead += read;
            }
            return data;
        }

        public void Write(string fileName, byte[] data)
        {
            using var fs = new FileStream(fileName, FileMode.Append);
            fs.Write(data, 0, data.Length);
        }
    }
}
