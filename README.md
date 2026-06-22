# Servidor Web Simple

Proyecto final - Programacion sobre redes

## Requisitos

- [.NET SDK](https://dotnet.microsoft.com/download) (8.0 o superior)
- Windows, Linux o macOS

## Clonar y ejecutar

```bash
git clone https://github.com/tu-usuario/servidor-web-tpf.git
cd servidor-web-tpf
dotnet run
```

El servidor muestra: `Servidor escuchando en puerto 8080`

## Archivo de configuracion

Editar `config.txt` para cambiar puerto y carpeta de archivos:

```
PORT=8080
ROOT=archivosEntregables
```

---

## Pruebas

Las pruebas se hacen con curl o curl.exe (viene instalado en Linux/macOS y Windows 10/11).
Las pruebas se pueden hacer desde la terminal o usando un navegador web.
Las pruebas con curl muestran claramente los headers y el cuerpo de la respuesta, ademas de permitir enviar datos POST y ver la compresion GZip.
Las pruebas asumen que el servidor esta corriendo en `http://localhost:8080/` y que hay archivos `prueba1.html` y `prueba2.html` en la carpeta `archivosEntregables/`.
Si las pruebas con curl no funcionan, probar con curl.exe en Windows.

### 1. GET - Pagina principal

```bash
curl http://localhost:8080/
```

Esperado: HTML con "Bienvenido! Este es el home!"

---

### 2. GET - Archivo especifico

```bash
curl http://localhost:8080/prueba1.html
curl http://localhost:8080/prueba2.html
```

---

### 3. GET - Archivo inexistente (error 404)

```bash
curl http://localhost:8080/noexiste.html
```

Esperado: HTML con codigo de error 404

---

### 4. GET - Con parametros en la URL

```bash
curl "http://localhost:8080/?nombre=Juan&edad=25"
```

En la consola del servidor se ven los parametros recibidos.

---

### 5. POST - Enviar datos

```bash
curl -X POST -d "usuario=test" http://localhost:8080/
```

En la consola del servidor aparece "POST body: usuario=test"

---

### 6. Compresion GZip

```bash
curl -H "Accept-Encoding: gzip" -v http://localhost:8080/ 2>&1
```

En los headers de respuesta debe aparecer: `Content-Encoding: gzip`

Para ver el contenido descomprimido directamente:

```bash
curl --compressed http://localhost:8080/
```

---

### 7. Ver logs

Los logs se guardan por dia en la carpeta `logs/`:

```bash
# Windows PowerShell
Get-ChildItem logs\
Get-Content logs\2026-06-21.txt

# Linux / macOS
ls logs/
cat logs/2026-06-21.txt
```

Formato de cada linea: `HH:mm:ss | IP | METODO | ruta`

---

### 8. Concurrencia

Abrir dos terminales y ejecutar al mismo tiempo:

```bash
# Terminal 1
curl http://localhost:8080/prueba1.html

# Terminal 2
curl http://localhost:8080/prueba2.html
```

Ambas responden sin esperar una a la otra.

---

## Otras formas de probar

### Navegador web

```
http://localhost:8080/
http://localhost:8080/prueba1.html
http://localhost:8080/noexiste.html
```

### PowerShell (solo Windows)

```powershell
Invoke-WebRequest -Uri http://localhost:8080/ -UseBasicParsing
Invoke-WebRequest -Uri http://localhost:8080/ -Method POST -Body "dato=test" -UseBasicParsing
```

### wget (Linux / macOS)

```bash
wget -q -O - http://localhost:8080/
```
