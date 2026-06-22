using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO.Compression;


/*Lectura del config.txt*/
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


/*Creación del socket del servidor
Socket servidor = new Socket(
    AddressFamily.InterNetwork,
    SocketType.Stream,
    ProtocolType.Tcp
);*/
var listener = new TcpListener(IPAddress.Any, puerto);

//IPAddress ip = IPAddress.Any;
listener.Start();

//IPEndPoint endpoint = new IPEndPoint(ip, puerto);

//servidor.Bind(endpoint);
//servidor.Listen(5);

Console.WriteLine("El servidor está escuchando el puerto {0}", puerto);


/*Creación de sockets para los clientes + Envio de respuesta*/
while (true)
{
    Console.WriteLine("Esperando un cliente...");
    //Socket cliente = servidor.Accept();listener.AcceptSocket()
    Socket cliente = listener.AcceptSocket();
    Console.WriteLine("Se conectó un cliente.");

    _ = Task.Run(() => AtenderCliente(cliente, carpetaArchivos));

}


static void AtenderCliente(Socket cliente, string carpetaArchivos)
{
    try
    {

        string ipOrigen = ((IPEndPoint)cliente.RemoteEndPoint).Address.ToString();

        /*Creación del buffer*/
        byte[] buffer = new byte[2500];
        int bytesRecibidos = cliente.Receive(buffer);
        string request = Encoding.UTF8.GetString(buffer, 0, bytesRecibidos);


        /*Parsear el request*/
        string[] lineasReq = request.Split("\r\n");
        string primerLinea = lineasReq[0];
        /*Guardando datos del request*/
        string[] partesReq = primerLinea.Split(" ");

        if (partesReq.Length < 2) { cliente.Close(); return; }

        string metodo = partesReq[0];
        string ruta = partesReq[1];

        if (ruta == "/") ruta = "/index.html";

        //string version = partesReq[2];
        string queryString = "";

        /*Separar los query param*/
        if (ruta.Contains("?"))
        {
            string[] partesUrl = ruta.Split('?', 2);
            ruta = partesUrl[0];
            if (ruta == "/") ruta = "/index.html";

            queryString = partesUrl[1];

            //for each para loggear los query param
            foreach (string par in queryString.Split('&'))
            {
                string[] p = par.Split('=');
                Console.WriteLine("Query: {0} = {1}", p[0], p.Length == 2 ? p[1] : "");
            }
        }

        /*Log de los query param
        if(!string.IsNullOrEmpty(queryString))
        {
            string[] parametros = queryString.Split('&');
            foreach (string par in parametros)
            {
                string[] parametro = par.Split('=');

                if (parametro.Length == 2)
                {
                    Console.WriteLine($"Parámetro: {parametro[0]} | Valor: {parametro[1]}");
                }
                else
                {
                    Console.WriteLine($"Parámetro: {parametro[0]} | Valor: ");
                }

            }
        }*/


        /*Loggin del POST*/
        //string body = "";

        if (metodo == "POST")
        {
            int indiceVacio = Array.IndexOf(lineasReq, "");
            if (indiceVacio != -1 && indiceVacio + 1 < lineasReq.Length)
            {
                //Console.WriteLine("POST body: {0}", lineasReq[idx + 1]);
                //body = lineasReq[indiceVacio + 1];
                Console.WriteLine("POST body: {0}", lineasReq[indiceVacio + 1]);
            }

            //Console.WriteLine("POST recibido:");
            //Console.WriteLine(body);
        }

        /*Loggin del GET*/
        if (metodo == "GET")
        {
            Console.WriteLine("GET request:");
            Console.WriteLine(ruta);
        }

        // Gzip
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
        File.AppendAllText(archivoLog, entrada + Environment.NewLine);


        /*Construir una ruta completa y segura*/
        string rutaBase = Path.GetFullPath(carpetaArchivos);
        string rutaCompleta = Path.GetFullPath(Path.Combine(rutaBase, ruta.TrimStart('/')));

        /*Validar la ruta completa*/
        if (!rutaCompleta.StartsWith(rutaBase))
        {
            //string body403 = "Acceso denegado";
            string body403 = "<!DOCTYPE html><html><head><meta charset='UTF-8'><title>403 - Acceso denegado</title><style>body{font-family:Arial,sans-serif;text-align:center;padding:50px;background:#f0f0f0}h1{color:#e67e22;font-size:60px;margin:0}p{color:#555;font-size:20px}a{color:#3498db;text-decoration:none}a:hover{text-decoration:underline}</style></head><body><h1>403</h1><p>No tenes permiso para acceder a este recurso.</p><a href='/'>Volver al inicio</a></body></html>";
            byte[] body403bytes = Encoding.UTF8.GetBytes(body403);
            string resp = "HTTP/1.1 403 Forbidden\r\nContent-Type: text/html\r\nContent-Length: " +
                body403bytes.Length + "\r\n\r\n";
            cliente.Send(Encoding.UTF8.GetBytes(resp));
            cliente.Send(body403bytes);
            cliente.Close();
            return;
        }


        /*Guardar la ruta del archivo*/
        string rutaArchivo = carpetaArchivos + ruta;
        FileInfo archivo = new FileInfo(rutaArchivo);


        /*Enviar respuesta según si existe o no el archivo*/
        if (archivo.Exists)
        {
            byte[] archivoBytes = File.ReadAllBytes(rutaArchivo);
            string extension = ObtenerTipoMime(rutaArchivo);


            /*string respuesta =
            "HTTP/1.1 200 OK\r\n" +
            $"Content-Type: {extension}\r\n" +
            $"Content-Length: {archivoBytes.Length}\r\n" +
            "\r\n";
            byte[] datosRespuesta = Encoding.UTF8.GetBytes(respuesta);*/
            //cliente.Send(datosRespuesta);
            //cliente.Send(archivoBytes);
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
            //cliente.Close();
        }
        else
        {
            /*string body404 = "Archivo no encontrado";

            string respuesta =
            "HTTP/1.1 404 Not Found\r\n" +
            "Content-Type: text/plain\r\n" +
            $"Content-Length: {Encoding.UTF8.GetByteCount(body404)}\r\n" +
            "\r\n" +
            body404;
        
            byte[] datosRespuesta = Encoding.UTF8.GetBytes(respuesta);

            cliente.Send(datosRespuesta);*/

            string body404 = "<!DOCTYPE html><html><head><meta charset='UTF-8'><title>404 - Pagina no encontrada</title><style>body{font-family:Arial,sans-serif;text-align:center;padding:50px;background:#f0f0f0}h1{color:#e74c3c;font-size:60px;margin:0}p{color:#555;font-size:20px}a{color:#3498db;text-decoration:none}a:hover{text-decoration:underline}</style></head><body><h1>404</h1><p>La pagina que buscas no existe en este servidor.</p><a href='/'>Volver al inicio</a></body></html>";
            byte[] b = Encoding.UTF8.GetBytes(body404);
            string resp = "HTTP/1.1 404 Not Found\r\nContent-Type: text/html\r\nContent-Length: " +
                b.Length + "\r\n\r\n";
            cliente.Send(Encoding.UTF8.GetBytes(resp));
            cliente.Send(b);

            //cliente.Close();
        }
        cliente.Close();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
        try { cliente.Close(); } catch { }
    }


}

static string ObtenerTipoMime(string rutaArchivo)
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