

using System;

namespace BeautyCare_API.Modelos
{
    public class Personal
    {
        public int PersonalID { get; set; }            // PK
        public string Nombre { get; set; } = "";       // NOT NULL
        public string? Apellido { get; set; }          // NULL
        public string Rol { get; set; } = "";          // NOT NULL
        public string? Telefono { get; set; }          // NULL
        public string? CorreoElectronico { get; set; } // NULL
        public DateTime? FechaIngreso { get; set; }    // NULL
        public bool Activo { get; set; }               // NOT NULL (DEFAULT 1)
    }
}
