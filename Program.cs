string[] lineas = File.ReadAllLines("config.txt");

int puerto = 0;
string carpeta = "";

foreach (string linea in lineas)
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