namespace BeautyCare_API.Modelos
{
    public class Servicio
    {
        public int ServicioID { get; set; }        // PK
        public string Nombre { get; set; } = "";   // NOT NULL
        public decimal Precio { get; set; }        // DECIMAL(10,2) NOT NULL
        public int DuracionMin { get; set; }       // NOT NULL
    }
}
