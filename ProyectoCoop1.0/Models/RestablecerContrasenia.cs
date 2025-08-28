using System.ComponentModel.DataAnnotations;

namespace ProyectoCoop1._0.Models
{
    public class RestablecerContrasenia
    {                     
            [Required(ErrorMessage = "Debe ingresar el nombre de usuario")]            
            public string usuarioLogin { get; set; }

            [Required]            
            public string email { get; set; }

            [Required]
            [DataType(DataType.Password)]            
            public string NuevaContrasenia { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("NuevaContrasenia", ErrorMessage = "Las contraseñas no coinciden")]            
            public string ConfirmarContrasenia { get; set; }
        }
    }