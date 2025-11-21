using BlogMVC.Entidades;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlogMVC.Datos
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        protected ApplicationDbContext()
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Filtro global para comentarios no borrados
            builder.Entity<Comentario>().HasQueryFilter(x => !x.Borrado);
            // Filtro global para entradas no borradas
            builder.Entity<Entrada>().HasQueryFilter(x => !x.Borrado);
        }

        public DbSet<Entrada> Entradas { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
    }
}
