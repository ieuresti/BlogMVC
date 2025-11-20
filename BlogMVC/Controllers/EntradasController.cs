using BlogMVC.Datos;
using BlogMVC.Entidades;
using BlogMVC.Models;
using BlogMVC.Servicios;
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
                .Include(x => x.UsuarioCreacion) // Incluir la data del usuario que creo la entrada
                .Include(x => x.Comentarios) // Incluir los comentarios de la entrada
                    .ThenInclude(x => x.Usuario) // Incluir la data del usuario que hizo cada comentario
                .FirstOrDefaultAsync(x => x.Id == id);
            if (entrada is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var modelo = new EntradaDetalleViewModel
            {
                Id = id,
                Titulo = entrada.Titulo,
                Cuerpo = entrada.Cuerpo,
                PortadaUrl = entrada.PortadaUrl,
                EscritoPor = entrada.UsuarioCreacion!.Nombre,
                FechaPublicacion = entrada.FechaPublicacion
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
    }
}
