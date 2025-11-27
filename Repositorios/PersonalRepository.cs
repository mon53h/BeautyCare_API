using BeautyCare_API.Data;
using BeautyCare_API.Repositorios;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BeautyCare_API.Repositorios
{
    public class PersonalRepository : BaseSpRepository
    {
        private const string SP = "MAN_PERSONAL";
        public PersonalRepository(AplicationsDbContext db) : base(db) { }

        public Task<DataSet> ListarAsync(int? personalId = null, string? rol = null, bool? activo = null)
            => EjecutarSpDataSetAsync(SP, 90, new[]
            {
                P("@PersonalID", SqlDbType.Int, personalId),
                P("@Rol",        SqlDbType.VarChar, rol, 50),
                P("@Activo",     SqlDbType.Bit, activo)
            });

        public async Task<int> CrearAsync(string nombre, string? apellido, string rol, string? tel,
                                          string? correo, DateTime? fechaIngreso, bool? activo = true)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 1, new[]
            {
                P("@PersonalID",       SqlDbType.Int, DBNull.Value),
                P("@Nombre",           SqlDbType.VarChar, nombre, 100),
                P("@Apellido",         SqlDbType.VarChar, apellido, 100),
                P("@Rol",              SqlDbType.VarChar, rol, 50),
                P("@Telefono",         SqlDbType.VarChar, tel, 20),
                P("@CorreoElectronico",SqlDbType.VarChar, correo, 100),
                P("@FechaIngreso",     SqlDbType.DateTime, fechaIngreso),
                P("@Activo",           SqlDbType.Bit, activo)
            });
            return ReadIdentityByName(ds, "PersonalID");
        }

        public async Task<int> ActualizarAsync(int id, string? nombre, string? apellido, string? rol, string? tel,
                                               string? correo, DateTime? fechaIngreso, bool? activo)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 2, new[]
            {
                P("@PersonalID",       SqlDbType.Int, id),
                P("@Nombre",           SqlDbType.VarChar, nombre, 100),
                P("@Apellido",         SqlDbType.VarChar, apellido, 100),
                P("@Rol",              SqlDbType.VarChar, rol, 50),
                P("@Telefono",         SqlDbType.VarChar, tel, 20),
                P("@CorreoElectronico",SqlDbType.VarChar, correo, 100),
                P("@FechaIngreso",     SqlDbType.DateTime, fechaIngreso),
                P("@Activo",           SqlDbType.Bit, activo)
            });
            return ReadInt(ds, "Afectados");
        }

        public async Task<int> EliminarAsync(int id)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 3, new[]
            {
                P("@PersonalID", SqlDbType.Int, id)
            });
            return ReadInt(ds, "Afectados");
        }
    }
}
