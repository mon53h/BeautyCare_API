using BeautyCare_API.Data;
using BeautyCare_API.Repositorios;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BeautyCare_API.Repositorios
{
    public class UsuariosRepository : BaseSpRepository
    {
        private const string SP = "MAN_USUARIOS";
        public UsuariosRepository(AplicationsDbContext db) : base(db) { }

        public Task<DataSet> ListarAsync(int? usuarioId = null, string? nombreUsuario = null, string? rol = null)
            => EjecutarSpDataSetAsync(SP, 90, new[]
            {
                P("@UsuarioID",     SqlDbType.Int, usuarioId),
                P("@NombreUsuario", SqlDbType.VarChar, nombreUsuario, 50),
                P("@Rol",           SqlDbType.VarChar, rol, 20)
            });

        public async Task<int> CrearAsync(string nombreUsuario, string contrasenaHash, string rol)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 1, new[]
            {
                P("@UsuarioID",     SqlDbType.Int, DBNull.Value),
                P("@NombreUsuario", SqlDbType.VarChar, nombreUsuario, 50),
                P("@ContrasenaHash",SqlDbType.VarChar, contrasenaHash, 255),
                P("@Rol",           SqlDbType.VarChar, rol, 20)
            });
            return ReadIdentityByName(ds, "UsuarioID");
        }

        public async Task<int> ActualizarAsync(int id, string? nombreUsuario, string? contrasenaHash, string? rol)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 2, new[]
            {
                P("@UsuarioID",     SqlDbType.Int, id),
                P("@NombreUsuario", SqlDbType.VarChar, nombreUsuario, 50),
                P("@ContrasenaHash",SqlDbType.VarChar, contrasenaHash, 255),
                P("@Rol",           SqlDbType.VarChar, rol, 20)
            });
            return ReadInt(ds, "Afectados");
        }

        public async Task<int> EliminarAsync(int id)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 3, new[]
            {
                P("@UsuarioID", SqlDbType.Int, id)
            });
            return ReadInt(ds, "Afectados");
        }
    }
}
