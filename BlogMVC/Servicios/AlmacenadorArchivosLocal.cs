
namespace BlogMVC.Servicios
{
    public class AlmacenadorArchivosLocal : IAlmacenadorArchivos
    {
        private readonly IWebHostEnvironment env; // Proporciona informacion de donde se encuentra mi wwwroot
        private readonly IHttpContextAccessor httpContextAccessor; // Permitira construir la URL en la cual se esta ejecutando la app

        public AlmacenadorArchivosLocal(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            this.env = env;
            this.httpContextAccessor = httpContextAccessor;
        }
        public async Task<string> Almacenar(string contenedor, IFormFile archivo)
        {
            // Extensión del archivo (p. ej. ".png", ".jpg").
            var extension = Path.GetExtension(archivo.FileName);
            // Genera un nombre único basado en GUID para evitar colisiones en el servidor.
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            // Carpeta física donde se almacenarán los archivos: wwwroot/{contenedor}
            string folder = Path.Combine(env.WebRootPath, contenedor);
            // Si la carpeta no existe, crearla. Directory.CreateDirectory es idempotente.
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            // Ruta física completa del archivo en disco.
            string ruta = Path.Combine(folder, nombreArchivo);
            // Leer el contenido del IFormFile en memoria y escribirlo en disco.
            // Nota: esto carga el archivo en memoria; para archivos grandes es mejor usar un FileStream y CopyToAsync directamente a disco para evitar usar mucha memoria.
            using (var ms = new MemoryStream())
            {
                await archivo.CopyToAsync(ms); // Copia el stream del archivo al MemoryStream
                var contenido = ms.ToArray(); // Obtiene el array de bytes
                await File.WriteAllBytesAsync(ruta, contenido); // Escribe el archivo en disco de forma asíncrona
            }
            // Construcción de la URL pública del archivo:
            // - Obtiene el esquema (http/https) y host (ej. localhost:5001) de la request actual.
            // - Combina con el contenedor y el nombre de archivo.
            // IMPORTANTE: Path.Combine usa "\" en Windows, por eso hacemos Replace("\\","/").
            var request = httpContextAccessor.HttpContext!.Request;
            var url = $"{request.Scheme}://{request.Host}";
            var urlArchivo = Path.Combine(url, contenedor, nombreArchivo).Replace("\\", "/");
            // Retorna la URL publica del archivo almacenado.
            return urlArchivo;
        }

        public Task Borrar(string ruta, string contenedor)
        {
            // Si la ruta es null/empty o sólo espacios, no hay nada que borrar: salir inmediatamente.
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return Task.CompletedTask;
            }
            // Obtener sólo el nombre del archivo a partir de la `ruta`.
            // `ruta` puede ser una URL (p.ej. https://host/contenedor/nombre.ext) o una ruta,
            // por eso usamos Path.GetFileName para extraer el nombre.
            var nombreArchivo = Path.GetFileName(ruta);
            // Construir la ruta física donde se espera encontrar el archivo en disco:
            // wwwroot/{contenedor}/{nombreArchivo}
            var directorioArchivo = Path.Combine(env.WebRootPath, contenedor, nombreArchivo);
            // Comprobar si el archivo existe antes de intentar borrarlo para evitar excepciones.
            if (File.Exists(directorioArchivo))
            {
                // Eliminar el archivo del sistema de archivos.
                File.Delete(directorioArchivo);
            }
            // Devolver una tarea ya completada (método síncrono dentro de una interfaz asíncrona).
            return Task.CompletedTask;
        }
    }
}
