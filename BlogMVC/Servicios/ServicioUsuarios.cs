using BlogMVC.Entidades;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace BlogMVC.Servicios
{
    public interface IServicioUsuarios
    {
        string? ObtenerUsuarioId();
    }
    public class ServicioUsuarios: IServicioUsuarios
    {
        private readonly HttpContext httpContext;
        private readonly UserManager<Usuario> userManager;

        public ServicioUsuarios(IHttpContextAccessor httpContextAccessor, UserManager<Usuario> userManager)
        {
            httpContext = httpContextAccessor.HttpContext!;
            this.userManager = userManager;
        }

        public string? ObtenerUsuarioId()
        {
            var idClaim = httpContext.User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault();
            if (idClaim is null)
            {
                return null;
            }
            return idClaim.Value;
        }
    }
}
