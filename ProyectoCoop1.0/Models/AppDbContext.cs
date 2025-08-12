using System;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ProyectoCoop1._0.Models
{
    public class AppDbContext : DbContext
    {
        // 👇 Este constructor permite a EF inyectar las opciones del contexto (cadena de conexión, etc.)
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }        

        // 👇 Estas propiedades indican a EF que cree tablas para estas entidades
        public DbSet<Socio> Socios { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

    }
}