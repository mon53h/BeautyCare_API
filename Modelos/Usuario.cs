namespace BeautyCare_API.Modelos
{
    public class Usuario
    {
        public int UsuarioID { get; set; }             // PK
        public string NombreUsuario { get; set; } = ""; // UNIQUE, NOT NULL
        public string ContrasenaHash { get; set; } = ""; // NOT NULL
        public string Rol { get; set; } = "";           // NOT NULL
    }
}
