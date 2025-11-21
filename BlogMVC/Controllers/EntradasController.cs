using BlogMVC.Datos;
using BlogMVC.Entidades;
using BlogMVC.Models;
using BlogMVC.Servicios;
using BlogMVC.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogMVC.Controllers
{
    public class EntradasController: Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly string contenedor = "entradas";

        public EntradasController(
            ApplicationDbContext context,
            IAlmacenadorArchivos almacenadorArchivos,
            IServicioUsuarios servicioUsuarios)
        {
            this.context = context;
            this.almacenadorArchivos = almacenadorArchivos;
            this.servicioUsuarios = servicioUsuarios;
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var entrada = await context.Entradas
                .IgnoreQueryFilters() // Ignorar los filtros globales para ver entradas borradas
                .Include(x => x.UsuarioCreacion) // Incluir la data del usuario que creo la entrada
                .Include(x => x.Comentarios) // Incluir los comentarios de la entrada
                    .ThenInclude(x => x.Usuario) // Incluir la data del usuario que hizo cada comentario
                .FirstOrDefaultAsync(x => x.Id == id);
            if (entrada is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            if (entrada.Borrado && context.UserRoles
                .FirstOrDefault(x => x.UserId == servicioUsuarios.ObtenerUsuarioId() &&
                                     x.RoleId == context.Roles
                                        .FirstOrDefault(r => r.Name == Constantes.RolAdmin)!.Id) is null)
            {
                var urlRetorno = HttpContext.ObtenerUrlRetorno();
                return RedirectToAction("Login", "Usuarios", new { urlRetorno });
            }

            // Comprobar si el usuario actual tiene el rol Admin o BorraComentarios
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();
            var puedeBorrarComentarios = false;
            if (!string.IsNullOrEmpty(usuarioId))
            {
                puedeBorrarComentarios = await context.UserRoles
                    .Join(context.Roles,
                          ur => ur.RoleId,
                          r => r.Id,
                          (ur, r) => new { UserRole = ur, Role = r })
                    .AnyAsync(x => x.UserRole.UserId == usuarioId &&
                                   (x.Role.Name == Constantes.RolAdmin || x.Role.Name == Constantes.BorraComentarios));
            }

            var modelo = new EntradaDetalleViewModel
            {
                Id = id,
                Titulo = entrada.Titulo,
                Cuerpo = entrada.Cuerpo,
                PortadaUrl = entrada.PortadaUrl,
                EscritoPor = entrada.UsuarioCreacion!.Nombre,
                FechaPublicacion = entrada.FechaPublicacion,
                EntradaBorrada = entrada.Borrado,
                Comentarios = entrada.Comentarios.Select(x => new ComentarioViewModel
                {
                    Id = x.Id,
                    Cuerpo = x.Cuerpo,
                    EscritoPor = x.Usuario!.Nombre,
                    FechaPublicacion = x.FechaPublicacion,
                    MostrarBotonBorrar = puedeBorrarComentarios || usuarioId == x.UsuarioId
                })
            };
            return View(modelo);
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = $"{Constantes.RolAdmin},{Constantes.CRUDEntradas}")]
        public async Task<IActionResult> Crear(EntradaCrearViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            string? portadaUrl = null;
            if (modelo.ImagenPortada is not null)
            {
                portadaUrl = await almacenadorArchivos.Almacenar(contenedor, modelo.ImagenPortada);
            }

            string usuarioId = servicioUsuarios.ObtenerUsuarioId();
            var entrada = new Entrada
            {
                Titulo = modelo.Titulo,
                Cuerpo = modelo.Cuerpo,
                PortadaUrl = portadaUrl,
                FechaPublicacion = DateTime.UtcNow,
                UsuarioCreacionId = usuarioId
            };

            context.Add(entrada);
            await context.SaveChangesAsync();
            return RedirectToAction("Detalle", new { id = entrada.Id });
        }

        [HttpGet]
        [Authorize(Roles = $"{Constantes.RolAdmin},{Constantes.CRUDEntradas}")]
        public async Task<IActionResult> Editar(int id)
        {
            var entrada = await context.Entradas.
                IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entrada is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var modelo = new EntradaEditarViewModel
            {
                Id = entrada.Id,
                Titulo = entrada.Titulo,
                Cuerpo = entrada.Cuerpo,
                ImagenPortadaActual = entrada.PortadaUrl
            };
            return View(modelo);
        }

        [HttpPost]
        [Authorize(Roles = $"{Constantes.RolAdmin},{Constantes.CRUDEntradas}")]
        public async Task<IActionResult> Editar(EntradaEditarViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }
            // Obtener la entrada de la base de datos
            var entradaDB = await context.Entradas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == modelo.Id);
            if (entradaDB is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            string? portadaUrl = null;
            if (modelo.ImagenPortada is not null)
            {
                // Si no es null, significa que el usuario subio una nueva imagen para cambiarla por la actual
                portadaUrl = await almacenadorArchivos.Editar(modelo.ImagenPortadaActual, contenedor, modelo.ImagenPortada);
            } else if (modelo.ImagenRemovida)
            {
                // El usuario decidio borrar la imagen actual sin subir una nueva
                await almacenadorArchivos.Borrar(modelo.ImagenPortadaActual, contenedor);
            } else
            {
                // El usuario decidio mantener la imagen actual
                portadaUrl = entradaDB.PortadaUrl;
            }

            string usuarioId = servicioUsuarios.ObtenerUsuarioId();
            // Actualizar los demas campos de la entrada
            entradaDB.Titulo = modelo.Titulo;
            entradaDB.Cuerpo = modelo.Cuerpo;
            entradaDB.PortadaUrl = portadaUrl;
            entradaDB.UsuarioActualizacionId = usuarioId;
            // Guardar los cambios en la bd
            await context.SaveChangesAsync();
            return RedirectToAction("Detalle", new { id = entradaDB.Id } );
        }

        [HttpPost]
        [Authorize(Roles = $"{Constantes.RolAdmin},{Constantes.CRUDEntradas}")]
        public async Task<IActionResult> Borrar(int id, bool borrado)
        {
            var entradaDB = await context.Entradas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
            if (entradaDB is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }
            entradaDB.Borrado = borrado;
            await context.SaveChangesAsync();
            return RedirectToAction("Detalle", new { id = entradaDB.Id } );
        }
    }
}
