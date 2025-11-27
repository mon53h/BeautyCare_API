using BeautyCare_API.Data;
using BeautyCare_API.Repositorios;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;  //  NECESARIO para GetDbTransaction()

namespace BeautyCare_API.Repositorios
{
    public class ClientesRepository : BaseSpRepository
    {
        private const string SP = "MAN_CLIENTES";
        public ClientesRepository(AplicationsDbContext db) : base(db) { }

        public Task<DataSet> ListarAsync(int? clienteId = null, string? nombre = null, string? apellidos = null, string? correo = null)
            => EjecutarSpDataSetAsync(SP, 90, new[]
            {
                P("@ClienteID",        SqlDbType.Int, clienteId),
                P("@Nombre",           SqlDbType.VarChar, nombre, 100),
                P("@Apellidos",        SqlDbType.VarChar, apellidos, 100),
                P("@CorreoElectronico",SqlDbType.VarChar, correo, 100)
            });

        public async Task<int> CrearAsync(string nombre, string? apellidos, string? tel, string? correo, DateTime? fechaReg = null)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 1, new[]
            {
                P("@ClienteID",        SqlDbType.Int, DBNull.Value),
                P("@Nombre",           SqlDbType.VarChar, nombre, 100),
                P("@Apellidos",        SqlDbType.VarChar, apellidos, 100),
                P("@Telefono",         SqlDbType.VarChar, tel, 20),
                P("@CorreoElectronico",SqlDbType.VarChar, correo, 100),
                P("@FechaRegistro",    SqlDbType.DateTime, fechaReg)
            });
            return ReadIdentityByName(ds, "ClienteID");
        }

        public async Task<int> ActualizarAsync(int id, string? nombre, string? apellidos, string? tel, string? correo)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 2, new[]
            {
                P("@ClienteID",        SqlDbType.Int, id),
                P("@Nombre",           SqlDbType.VarChar, nombre, 100),
                P("@Apellidos",        SqlDbType.VarChar, apellidos, 100),
                P("@Telefono",         SqlDbType.VarChar, tel, 20),
                P("@CorreoElectronico",SqlDbType.VarChar, correo, 100)
            });
            return ReadInt(ds, "Afectados");
        }

        public async Task<int> EliminarAsync(int id)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 3, new[]
            {
                P("@ClienteID", SqlDbType.Int, id)
            });
            return ReadInt(ds, "Afectados");
        }
    }
}
