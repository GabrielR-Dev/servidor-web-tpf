using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;


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


/*Creación del socket del servidor*/
Socket servidor = new Socket(
    AddressFamily.InterNetwork,
    SocketType.Stream,
    ProtocolType.Tcp
);

IPAddress ip = IPAddress.Any;
IPEndPoint endpoint = new IPEndPoint(ip, puerto);

servidor.Bind(endpoint);
servidor.Listen(5);

Console.WriteLine("El servidor está escuchando el puerto {0}", puerto);


/*Creación de sockets para los clientes + Envio de respuesta*/
while (true)
{
    Console.WriteLine("Esperando un cliente...");

    Socket cliente = servidor.Accept();

    Console.WriteLine("Se conectó un cliente.");


    /*Creación del buffer*/
    byte[] buffer = new byte[2500];
    int bytesRecibidos = cliente.Receive(buffer);
    string request = Encoding.UTF8.GetString(buffer, 0, bytesRecibidos);
    

    /*Parsear el request*/
    string[] lineasReq = request.Split("\r\n");
    string primerLinea = lineasReq[0];


    /*Guardando datos del request*/
    string[] partesReq = primerLinea.Split(" ");
    string metodo = partesReq[0];
    string ruta = partesReq[1];
    if (ruta == "/")
    {
        ruta = "/index.html";
    }
    string version = partesReq[2];
    string queryString = "";

    /*Separar los query param*/
    if (ruta.Contains("?"))
    {
        string[] partesUrl = ruta.Split('?', 2);
        ruta = partesUrl[0];
        queryString = partesUrl[1];
    }

    /*Log de los query param*/
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
    }


    /*Loggin del POST*/
    string body = "";

    if (metodo == "POST")
    {
        int indiceVacio = Array.IndexOf(lineasReq, "");
        
        if (indiceVacio != -1 && indiceVacio + 1 < lineasReq.Length)
        {
            body = lineasReq[indiceVacio + 1];
        }

        Console.WriteLine("POST recibido:");
        Console.WriteLine(body);
    }

    /*Loggin del GET*/
    if (metodo == "GET")
    {
        Console.WriteLine("GET request:");
        Console.WriteLine(ruta);
    }


    /*Construir una ruta completa y segura*/
    string rutaBase = Path.GetFullPath(carpetaArchivos);
    string rutaCompleta = Path.GetFullPath(Path.Combine(rutaBase, ruta.TrimStart('/')));

    /*Validar la ruta completa*/
    if (!rutaCompleta.StartsWith(rutaBase))
    {
        string body403 = "Acceso denegado";

        string respuesta =
        "HTTP/1.1 404 Not Found\r\n" +
        "Content-Type: text/plain\r\n" +
        $"Content-Length: {Encoding.UTF8.GetByteCount(body403)}\r\n" +
        "\r\n" +
        body403;
        
        byte[] datosRespuesta = Encoding.UTF8.GetBytes(respuesta);

        cliente.Send(datosRespuesta);
        cliente.Close();

        continue;
    }


    /*Guardar la ruta del archivo*/
    string rutaArchivo = carpetaArchivos + ruta;
    FileInfo archivo = new FileInfo(rutaArchivo);


    /*Obtener la extension del archivo*/
    static string ObtenerTipoMime(string rutaArchivo)
    {
        string extension = Path.GetExtension(rutaArchivo).ToLower();

        switch(extension)
        {
            case ".html":
            case ".htm":
            return "text/html";

            case ".css":
            return "text/css";

            case ".js":
            return "application/javascript";

            case ".png":
            return "image/png";

            case ".jpg":
            case ".jpeg":
            return "image/jpeg";

            case ".gif":
            return "image/gif";

            case ".txt":
            return "text/plain";

            default:
            return "application/octet-stream";
        }
    }


    /*Enviar respuesta según si existe o no el archivo*/
    if (archivo.Exists)
    {
        string extension = ObtenerTipoMime(rutaArchivo);

        byte[] archivoBytes = File.ReadAllBytes(rutaArchivo);

        string respuesta =
        "HTTP/1.1 200 OK\r\n" +
        $"Content-Type: {extension}\r\n" +
        $"Content-Length: {archivoBytes.Length}\r\n" +
        "\r\n";
        
        byte[] datosRespuesta = Encoding.UTF8.GetBytes(respuesta);

        cliente.Send(datosRespuesta);
        cliente.Send(archivoBytes);
        cliente.Close();
    }
    else
    {
        string body404 = "Archivo no encontrado";

        string respuesta =
        "HTTP/1.1 404 Not Found\r\n" +
        "Content-Type: text/plain\r\n" +
        $"Content-Length: {Encoding.UTF8.GetByteCount(body404)}\r\n" +
        "\r\n" +
        body404;
        
        byte[] datosRespuesta = Encoding.UTF8.GetBytes(respuesta);

        cliente.Send(datosRespuesta);
        cliente.Close();
    }

}