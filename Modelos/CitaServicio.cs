namespace BeautyCare_API.Modelos
{
    public class CitaServicio
    {
        public int CitaID { get; set; }      // FK -> Citas
        public int ServicioID { get; set; }  // FK -> Servicios
    }
}
