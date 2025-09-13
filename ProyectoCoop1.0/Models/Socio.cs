using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ProyectoCoop1._0.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCoop1._0.Models
{
    public class Socio
    {
        [Key]
        public int id { get; set; }

        [Required]
        public string nombre { get; set; }

        [Required]
        public string apellido { get; set; }

        [Required]
        [EmailAddress]
        public string email { get; set; }

        [Required]
        public string telefono { get; set; }

        [Required]
        public string direccion { get; set; }

        
        [Required]
        [MaxLength(50)]
        public string usuarioLogin { get; set; } = string.Empty;//propiedad para login y evita que se null
        

        public int? UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }//propiedad para recorrer todos las propiedades

        [ValidateNever]
        public ICollection<Turno> Turnos { get; set; }
    }
}
