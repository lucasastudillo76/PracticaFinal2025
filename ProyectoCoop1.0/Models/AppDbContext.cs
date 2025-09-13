using System;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ProyectoCoop1._0.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)//constructor para cadena de conexion
        {
        }        
 
        public DbSet<Socio> Socios { get; set; }//tablas de cada uno
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

    }
}