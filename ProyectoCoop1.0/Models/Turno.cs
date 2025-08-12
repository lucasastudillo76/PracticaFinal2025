using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ProyectoCoop1._0.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCoop1._0.Models

{
    public class Turno
    {
        [Key]
        public int id { get; set; }                

        [Required]
        public DateTime FechaHora { get; set; }

        [Required]
        public string servicio { get; set; }

        [Required]
        public string tarea { get; set; }

        [Required]
        public string estado { get; set; }

        public int socioId { get; set; }
        [ForeignKey("socioId")]

        [ValidateNever]
        public Socio Socio { get; set; }
    }
}
