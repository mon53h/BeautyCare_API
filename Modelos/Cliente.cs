
using System;

namespace BeautyCare_API.Modelos
{
    public class Cliente
    {
        public int ClienteID { get; set; }                  // PK
        public string Nombre { get; set; } = "";            // NOT NULL
        public string? Apellidos { get; set; }              // NULL
        public string? Telefono { get; set; }               // NULL
        public string? CorreoElectronico { get; set; }      // NULL
        public DateTime FechaRegistro { get; set; }         // NOT NULL (DEFAULT GETDATE())
    }
}
