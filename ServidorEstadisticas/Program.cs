using ServidorEstadisticas.Servicios;

Console.WriteLine("Servidor de Estadísticas iniciado.");
Console.WriteLine("Esperando eventos desde el servidor principal...");

var receptor = new EventoReceiver();
await receptor.StartAsync();
