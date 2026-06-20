using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;


/*Lectura del config.txt*/
string[] lineasConfig = File.ReadAllLines("config.txt");

int puerto = 0;
string carpeta = "";

foreach (string linea in lineasConfig)
{
    string[] partes = linea.Split('=');
    if (partes[0] == "PORT")
    {
        puerto = int.Parse(partes[1]);
    }
    else if (partes[0] == "ROOT")
    {
        carpeta = partes[1];
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


/*Creación de sockets para los clientes*/
while (true)
{
    Console.WriteLine("Esperando un cliente...");

    Socket cliente = servidor.Accept();

    Console.WriteLine("Se conectó un cliente.");

    /*Creación del buffer*/
    byte[] buffer = new byte[2500];
    int bytesRecibidos = cliente.Receive(buffer);
    string request = Encoding.UTF8.GetString(buffer, 0, bytesRecibidos);
    /*Console.WriteLine(request);*/
    
    /*Parsear el request*/
    string[] lineasReq = request.Split("\r\n");
    string primerLinea = lineasReq[0];
    Console.WriteLine(primerLinea);

    string[] partesReq = primerLinea.Split(" ");
    string metodo = partesReq[0];
    string ruta = partesReq[1];
    string version = partesReq[2];


    /*Enviar respuesta*/
    string respuesta =
    "HTTP/1.1 200 OK\r\n" +
    "Content-Type: text/html\r\n" +
    "\r\n" +
    "<h1>Texto de prueba</h1>";
    byte[] datosRespuesta = Encoding.UTF8.GetBytes(respuesta);

    cliente.Send(datosRespuesta);
    cliente.Close();

}