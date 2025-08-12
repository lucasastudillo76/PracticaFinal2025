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

        // 🆕 Campo para login único
        [Required]
        [MaxLength(50)]
        public string usuarioLogin { get; set; } = string.Empty;
        // El campo único que se usará para iniciar sesión

        public int? UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        [ValidateNever]
        public ICollection<Turno> Turnos { get; set; }
    }
}
