namespace ProyectoCoop1._0.Models
{
    public class Usuario
    {      
            public int id { get; set; }
            public string usuario { get; set; } = null!;
            public string contrasenia { get; set; } = null!;
            public bool esadmin { get; set; }        

    }
}
