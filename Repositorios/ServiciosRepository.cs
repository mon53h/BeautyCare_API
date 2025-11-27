using BeautyCare_API.Data;
using BeautyCare_API.Repositorios;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BeautyCare_API.Repositorios
{
    public class ServiciosRepository : BaseSpRepository
    {
        private const string SP = "MAN_SERVICIOS";
        public ServiciosRepository(AplicationsDbContext db) : base(db) { }

        public Task<DataSet> ListarAsync(int? servicioId = null, string? nombre = null)
            => EjecutarSpDataSetAsync(SP, 90, new[]
            {
                P("@ServicioID", SqlDbType.Int, servicioId),
                P("@Nombre",     SqlDbType.VarChar, nombre, 100)
            });

        public async Task<int> CrearAsync(string nombre, decimal precio, int duracionMin)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 1, new[]
            {
                P("@ServicioID", SqlDbType.Int, DBNull.Value),
                P("@Nombre",     SqlDbType.VarChar, nombre, 100),
                P("@Precio",     SqlDbType.Decimal, precio),
                P("@DuracionMin",SqlDbType.Int, duracionMin)
            });
            return ReadIdentityByName(ds, "ServicioID");
        }

        public async Task<int> ActualizarAsync(int id, string? nombre, decimal? precio, int? duracionMin)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 2, new[]
            {
                P("@ServicioID", SqlDbType.Int, id),
                P("@Nombre",     SqlDbType.VarChar, nombre, 100),
                P("@Precio",     SqlDbType.Decimal, precio),
                P("@DuracionMin",SqlDbType.Int, duracionMin)
            });
            return ReadInt(ds, "Afectados");
        }

        public async Task<int> EliminarAsync(int id)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 3, new[]
            {
                P("@ServicioID", SqlDbType.Int, id)
            });
            return ReadInt(ds, "Afectados");
        }
    }
}
