using BeautyCare_API.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BeautyCare_API.Repositorios
{
    public class LoginRepository : BaseSpRepository
    {
        private const string SP_AUTH = "AUTH_LOGIN";   // @NombreUsuario, @Contrasena (TEXTO)
        private const string SP_USR = "MAN_USUARIOS"; // CRUD usuarios

        public LoginRepository(AplicationsDbContext db) : base(db) { }

        //public Task<DataSet> AutenticarAsync(string nombreUsuario, string contrasenaTexto)
        //    => EjecutarSpDataSetAsync(SP_AUTH, 90, new[]
        //    {
        //        new SqlParameter("@NombreUsuario", SqlDbType.VarChar, 50){ Value = nombreUsuario },
        //        new SqlParameter("@Contrasena",    SqlDbType.NVarChar,200){ Value = contrasenaTexto }
        //    });


        public Task<DataSet> AutenticarAsync(string nombreUsuario, string contrasenaTexto)
    => EjecutarSpDataSetSinProcesoAsync(SP_AUTH, new[]
    {
        new SqlParameter("@NombreUsuario", SqlDbType.VarChar, 50){ Value = nombreUsuario },
        new SqlParameter("@Contrasena",    SqlDbType.NVarChar,200){ Value = contrasenaTexto }
    });


        public Task<DataSet> ConsultarAsync(int? usuarioId = null, string? nombreUsuario = null, string? rol = null)
            => EjecutarSpDataSetAsync(SP_USR, 90, new[]
            {
                P("@UsuarioID",     SqlDbType.Int, usuarioId),
                P("@NombreUsuario", SqlDbType.VarChar, nombreUsuario, 50),
                P("@Rol",           SqlDbType.VarChar, rol, 20)
            });

        public async Task<int> CrearAsync(string nombreUsuario, string contrasenaHash, string rol)
        {
            var ds = await EjecutarSpDataSetAsync(SP_USR, 1, new[]
            {
                P("@UsuarioID",     SqlDbType.Int, DBNull.Value),
                P("@NombreUsuario", SqlDbType.VarChar, nombreUsuario, 50),
                P("@ContrasenaHash",SqlDbType.VarChar, contrasenaHash, 255),
                P("@Rol",           SqlDbType.VarChar, rol, 20)
            });
            return ReadIdentityByName(ds, "UsuarioID");
        }

        public async Task<int> ActualizarAsync(int usuarioId, string? nombreUsuario, string? contrasenaHash, string? rol)
        {
            var ds = await EjecutarSpDataSetAsync(SP_USR, 2, new[]
            {
                P("@UsuarioID",     SqlDbType.Int, usuarioId),
                P("@NombreUsuario", SqlDbType.VarChar, nombreUsuario, 50),
                P("@ContrasenaHash",SqlDbType.VarChar, contrasenaHash, 255),
                P("@Rol",           SqlDbType.VarChar, rol, 20)
            });
            return ReadInt(ds, "Afectados");
        }

        public async Task<int> EliminarAsync(int usuarioId)
        {
            var ds = await EjecutarSpDataSetAsync(SP_USR, 3, new[]
            {
                P("@UsuarioID", SqlDbType.Int, usuarioId)
            });
            return ReadInt(ds, "Afectados");
        }
    }
}
