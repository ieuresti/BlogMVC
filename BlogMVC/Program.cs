using BlogMVC.Datos;
using BlogMVC.Entidades;
using BlogMVC.Servicios;
using BlogMVC.Utilidades;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();
builder.Services.AddTransient<IServicioUsuarios, ServicioUsuarios>();
// Para inyectar DbContext en Blazor es recomendable usar AddDbContextFactory
builder.Services.AddDbContextFactory<ApplicationDbContext>(opciones => opciones.UseSqlServer("name=DefaultConnection")
// El seeding es el proceso de introducir datos iniciales o imprescindibles en la bd al arrancar la aplicación(ej roles, usuarios admin, datos de ej).Garantiza que la app tenga la config mínima necesaria para funcionar.
// La clase Seeding inserta una lista de roles en la bd en la tabla de IdentityRole si no existen.
.UseSeeding(Seeding.Aplicar)
.UseAsyncSeeding(Seeding.AplicarAsync)
);
// Configurar Identity
builder.Services.AddIdentity<Usuario, IdentityRole>(opciones =>
{
    opciones.SignIn.RequireConfirmedAccount = false;
}).AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
// Configurar URLs por defecto del login
builder.Services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, opciones =>
{
    opciones.LoginPath = "/usuarios/login";
    opciones.AccessDeniedPath = "/usuarios/login";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
