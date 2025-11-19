using BlogMVC.Datos;
using BlogMVC.Entidades;
using BlogMVC.Models;
using BlogMVC.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogMVC.Controllers
{
    public class UsuariosController: Controller
    {
        private readonly UserManager<Usuario> userManager;
        private readonly SignInManager<Usuario> signInManager;
        private readonly ApplicationDbContext applicationDbContext;

        public UsuariosController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager,
            ApplicationDbContext applicationDbContext)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.applicationDbContext = applicationDbContext;
        }

        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Registro(RegistroViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }
            var usuario = new Usuario() { Email = modelo.Email, UserName = modelo.Email, Nombre = modelo.Nombre };
            var resultado = await userManager.CreateAsync(usuario, password: modelo.Password);
            if (resultado.Succeeded)
            {
                await signInManager.SignInAsync(usuario, isPersistent: true);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(modelo);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string mensaje = null)
        {
            if (mensaje is not null)
            {
                ViewData["mensaje"] = mensaje;
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken] // recomendado: proteger contra CSRF
        public async Task<IActionResult> Login(LoginViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }
            var resultado = await signInManager.PasswordSignInAsync(modelo.Email, modelo.Password, modelo.Recuerdame, lockoutOnFailure: false);
            if (resultado.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Nombre de usuario o password incorrecto");
                return View(modelo);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // recomendado: proteger contra CSRF
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize(Roles = Constantes.RolAdmin)]
        public async Task<IActionResult> Listado(string? mensaje = null)
        {
            var usuarios = await applicationDbContext.Users.Select(u => new UsuarioViewModel
            {
                Id = u.Id,
                Email = u.Email
            }).ToListAsync();

            var modelo = new UsuariosListadoViewModel();
            modelo.Usuarios = usuarios;
            modelo.Mensaje = mensaje;
            return View(modelo);
        }

        [HttpGet]
        [Authorize(Roles = Constantes.RolAdmin)]
        public async Task<IActionResult> RolesUsuario(string usuarioId)
        {
            var usuario = await userManager.FindByIdAsync(usuarioId);
            if (usuario is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var rolesQueElUsuarioTiene = await userManager.GetRolesAsync(usuario);
            var rolesExistentes = await applicationDbContext.Roles.ToListAsync();
            var rolesDelUsuario = rolesExistentes.Select(x => new UsuarioRolViewModel
            {
                Nombre = x.Name!,
                LoTiene = rolesQueElUsuarioTiene.Contains(x.Name!)
            });

            var modelo = new UsuariosRolesUsuarioViewModel
            {
                UsuarioId = usuario.Id,
                Email = usuario.Email!,
                Roles = rolesDelUsuario.OrderBy(x => x.Nombre)
            };
            return View(modelo);
        }

        [HttpPost]
        [Authorize(Roles = Constantes.RolAdmin)]
        public async Task<IActionResult> EditarRoles(EditarRolesViewModel modelo)
        {
            var usuario = await userManager.FindByIdAsync(modelo.UsuarioId);
            if (usuario is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await applicationDbContext.UserRoles.Where(x => x.UserId == usuario.Id).ExecuteDeleteAsync();
            await userManager.AddToRolesAsync(usuario, modelo.RolesSeleccionados);

            var mensaje = $"Los roles del usuario {usuario.Email} se han actualizado correctamente.";
            return RedirectToAction("Listado", new { mensaje });
        }
    }
}
