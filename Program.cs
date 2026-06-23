using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO.Compression;

object lockLog = new object();


string[] lineasConfig = File.ReadAllLines("config.txt");

int puerto = 0;
string carpetaArchivos = "";

foreach (string linea in lineasConfig)
{
    string[] partes = linea.Split('=');
    if (partes[0] == "PORT")
    {
        puerto = int.Parse(partes[1]);
    }
    else if (partes[0] == "ROOT")
    {
        carpetaArchivos = partes[1];
    }
}



var listener = new TcpListener(IPAddress.Any, puerto);

listener.Start();

Console.WriteLine("El servidor está escuchando el puerto {0}", puerto);



while (true)
{
    Console.WriteLine("Esperando un cliente...");
    Socket cliente = listener.AcceptSocket();
    Console.WriteLine("Se conectó un cliente.");

    _ = Task.Run(() => AtenderCliente(cliente, carpetaArchivos, lockLog));
}


static void AtenderCliente(Socket cliente, string carpetaArchivos, object lockLog)
{
    try
    {
        string ipOrigen = ((IPEndPoint)cliente.RemoteEndPoint).Address.ToString();


        byte[] buffer = new byte[2500];
        int bytesRecibidos = cliente.Receive(buffer);
        string request = Encoding.UTF8.GetString(buffer, 0, bytesRecibidos);



        string[] lineasReq = request.Split("\r\n");
        string primerLinea = lineasReq[0];
        string[] partesReq = primerLinea.Split(" ");

        if (partesReq.Length < 2) { cliente.Close(); return; }

        string metodo = partesReq[0];
        string ruta = partesReq[1];

        if (ruta == "/") ruta = "/index.html";
        
        string queryString = "";


        if (ruta.Contains("?"))
        {
            string[] partesUrl = ruta.Split('?', 2);
            ruta = partesUrl[0];
            if (ruta == "/") ruta = "/index.html";

            queryString = partesUrl[1];


            foreach (string par in queryString.Split('&'))
            {
                string[] p = par.Split('=');
                Console.WriteLine("Query: {0} = {1}", p[0], p.Length == 2 ? p[1] : "");
            }
        }



        if (metodo == "POST")
        {
            int indiceVacio = Array.IndexOf(lineasReq, "");
            if (indiceVacio != -1 && indiceVacio + 1 < lineasReq.Length)
            {
                Console.WriteLine("POST body: {0}", lineasReq[indiceVacio + 1]);
            }
        }


        if (metodo == "GET")
        {
            Console.WriteLine("GET request:");
            Console.WriteLine(ruta);
        }


        bool aceptaGzip = false;

        foreach (string linea in lineasReq)
            if (linea.StartsWith("Accept-Encoding:", StringComparison.OrdinalIgnoreCase)
                && linea.Contains("gzip"))
            { aceptaGzip = true; break; }


        string dirLogs = "logs";
        Directory.CreateDirectory(dirLogs);

        string archivoLog = Path.Combine(dirLogs, DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
        string entrada = string.Format("{0:HH:mm:ss} | {1} | {2} | {3}",
            DateTime.Now, ipOrigen, metodo, ruta);


        if (!string.IsNullOrEmpty(queryString)) entrada += " | " + queryString;

        lock(lockLog)
        {
           File.AppendAllText(archivoLog, entrada + Environment.NewLine);
        }



        string rutaBase = Path.GetFullPath(carpetaArchivos);
        string rutaCompleta = Path.GetFullPath(Path.Combine(rutaBase, ruta.TrimStart('/')));


        if (!rutaCompleta.StartsWith(rutaBase))
        {
            string body403 = "<!DOCTYPE html><html><head><meta charset='UTF-8'><title>403 - Acceso denegado</title></head><body><h1>403</h1><p>No tenes permiso para acceder a este recurso.</p><a href='/'>Volver al inicio</a></body></html>";
            byte[] body403bytes = Encoding.UTF8.GetBytes(body403);
            string resp = "HTTP/1.1 403 Forbidden\r\nContent-Type: text/html\r\nContent-Length: " +
                body403bytes.Length + "\r\n\r\n";

            cliente.Send(Encoding.UTF8.GetBytes(resp));
            cliente.Send(body403bytes);
            cliente.Close();
            return;
        }



        string rutaArchivo = carpetaArchivos + ruta;
        FileInfo archivo = new FileInfo(rutaArchivo);



        if (archivo.Exists)
        {
            byte[] archivoBytes = File.ReadAllBytes(rutaArchivo);
            string extension = ObtenerExtension(rutaArchivo);

            if (aceptaGzip)
            {
                using (var ms = new MemoryStream())
                {
                    using (var gzip = new GZipStream(ms, CompressionLevel.Fastest))
                        gzip.Write(archivoBytes, 0, archivoBytes.Length);
                    byte[] comp = ms.ToArray();
                    string resp = "HTTP/1.1 200 OK\r\nContent-Encoding: gzip\r\nContent-Type: " +
                        extension + "\r\nContent-Length: " + comp.Length + "\r\n\r\n";

                    cliente.Send(Encoding.UTF8.GetBytes(resp));
                    cliente.Send(comp);
                }
            }
            else
            {
                string respuesta =
                "HTTP/1.1 200 OK\r\n" +
                $"Content-Type: {extension}\r\n" +
                $"Content-Length: {archivoBytes.Length}\r\n" +
                "\r\n";
                byte[] datosRespuesta = Encoding.UTF8.GetBytes(respuesta);

                cliente.Send(datosRespuesta);
                cliente.Send(archivoBytes);
            }
        }
        else
        {
            string body404 = "<!DOCTYPE html><html><head><meta charset='UTF-8'><title>404 - Pagina no encontrada</title></head><body><h1>404</h1><p>La pagina que buscas no existe en este servidor.</p><a href='/'>Volver al inicio</a></body></html>";
            byte[] b = Encoding.UTF8.GetBytes(body404);
            string resp = "HTTP/1.1 404 Not Found\r\nContent-Type: text/html\r\nContent-Length: " +
                b.Length + "\r\n\r\n";

            cliente.Send(Encoding.UTF8.GetBytes(resp));
            cliente.Send(b);
        }
        cliente.Close();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
        try { cliente.Close(); } catch { }
    }
}


static string ObtenerExtension(string rutaArchivo)
{
    string ext = Path.GetExtension(rutaArchivo).ToLower();
    switch (ext)
    {
        case ".html": case ".htm": return "text/html";
        case ".css": return "text/css";
        case ".js": return "application/javascript";
        case ".png": return "image/png";
        case ".jpg": case ".jpeg": return "image/jpeg";
        case ".gif": return "image/gif";
        case ".txt": return "text/plain";
        default: return "application/octet-stream";
    }
}