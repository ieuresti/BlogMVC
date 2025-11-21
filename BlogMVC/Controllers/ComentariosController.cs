using BlogMVC.Datos;
using BlogMVC.Entidades;
using BlogMVC.Models;
using BlogMVC.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogMVC.Controllers
{
    public class ComentariosController: Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IServicioUsuarios servicioUsuarios;

        public ComentariosController(
            ApplicationDbContext context,
            IServicioUsuarios servicioUsuarios)
        {
            this.context = context;
            this.servicioUsuarios = servicioUsuarios;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Comentar(EntradasComentarViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Detalle", "Entradas", new { id = modelo.Id });
            }

            var existeEntrada = await context.Entradas.AnyAsync(x => x.Id == modelo.Id);
            if (!existeEntrada)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var usuarioId = servicioUsuarios.ObtenerUsuarioId();
            var comentario = new Comentario
            {
                EntradaId = modelo.Id,
                Cuerpo = modelo.Cuerpo,
                UsuarioId = usuarioId,
                FechaPublicacion = DateTime.UtcNow
            };
            context.Add(comentario);
            await context.SaveChangesAsync();
            return RedirectToAction("Detalle", "Entradas", new { id = modelo.Id });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Borrar(int id)
        {
            var comentario = await context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);
            if (comentario is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            return View(comentario);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> BorrarComentario(int id)
        {
            var comentario = await context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);
            if (comentario is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            comentario.Borrado = true;
            await context.SaveChangesAsync();
            return RedirectToAction("Detalle", "Entradas", new { id = comentario.EntradaId });
        }
    }
}
