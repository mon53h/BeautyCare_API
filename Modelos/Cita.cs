
using System;


namespace BeautyCare_API.Modelos
{
    public class Cita
    {
        public int CitaID { get; set; }                 // PK
        public int ClienteID { get; set; }              // FK -> Clientes
        public int PersonalID { get; set; }             // FK -> Personal
        public DateTime FechaHoraInicio { get; set; }   // NOT NULL
        public DateTime? FechaHoraFin { get; set; }     // NULL
        public string Estado { get; set; } = "Agendada"; // CHECK ('Agendada','Completada','Cancelada','NoShow')
        public string? Descripcion { get; set; }        // NULL
        public string? Notas { get; set; }              // NULL
    }
   // public enum EstadoCita
   // {
   //     Agendada,
   //     Completada,
   //     Cancelada,
   //     NoShow
   // }
}
