using BlogMVC.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlogMVC.Utilidades
{
    /// Clase de ayuda para insertar datos iniciales relacionados con Identity (roles) en la base de datos.
    public static class Seeding
    {
        // Lista de roles que queremos asegurar existan en la base de datos.
        // Se usa Constantes para centralizar los nombres y evitar strings duplicados.
        private static List<string> roles = new List<string>
        {
            Constantes.RolAdmin,
            Constantes.CRUDEntradas,
            Constantes.BorraComentarios
        };

        /// Versión síncrona del seeding de roles.
        /// - Recorre la lista de roles y crea cada rol si no existe.
        /// - Recibe un DbContext genérico para permitir usar diferentes contextos que expongan IdentityRole.
        /// - El parámetro booleano se ignora (firma para compatibilidad con frameworks de seeding).
        public static void Aplicar(DbContext context, bool _)
        {
            foreach (var rol in roles)
            {
                // Buscamos por nombre para comprobar si el rol ya existe (evita duplicados).
                var rolDB = context.Set<IdentityRole>().FirstOrDefault(x => x.Name == rol);
                if (rolDB is null)
                {
                    // Si no existe, lo creamos. NormalizedName se usa por Identity para comparaciones.
                    context.Set<IdentityRole>().Add(new IdentityRole
                    {
                        Name = rol,
                        NormalizedName = rol.ToUpper()
                    });
                    // Guardamos los cambios inmediatamente.
                    context.SaveChanges();
                }
            }
        }

        /// Versión asíncrona del seeding de roles.
        /// - Funciona igual que la versión síncrona pero usa operaciones asincrónicas de EF Core.
        /// - Acepta un CancellationToken para permitir cancelar la operación si es necesario (por ejemplo, durante el shutdown).
        public static async Task AplicarAsync(DbContext context, bool _, CancellationToken cancellationToken)
        {
            foreach (var rol in roles)
            {
                // Versión asíncrona de la búsqueda.
                var rolDB = await context.Set<IdentityRole>().FirstOrDefaultAsync(x => x.Name == rol);
                if (rolDB is null)
                {
                    context.Set<IdentityRole>().Add(new IdentityRole
                    {
                        Name = rol,
                        NormalizedName = rol.ToUpper()
                    });
                    // Guardado asíncrono con el token de cancelación.
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}
