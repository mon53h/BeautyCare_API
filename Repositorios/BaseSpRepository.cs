using BeautyCare_API.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BeautyCare_API.Repositorios
{
    public abstract class BaseSpRepository
    {
        protected readonly AplicationsDbContext _context;
        protected BaseSpRepository(AplicationsDbContext db) => _context = db;

        /// Ejecuta un SP con @PROCESO y SIEMPRE devuelve DataSet
        protected async Task<DataSet> EjecutarSpDataSetAsync(string sp, int proceso, IEnumerable<SqlParameter> parametros)
        {
            var ds = new DataSet();
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = sp;

                cmd.Parameters.Add(new SqlParameter("@PROCESO", SqlDbType.TinyInt) { Value = proceso });
                foreach (var p in parametros) cmd.Parameters.Add(p);

                await _context.Database.OpenConnectionAsync();
                using var da = new SqlDataAdapter((SqlCommand)cmd);
                await Task.Run(() => da.Fill(ds));
                return ds;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }






        // Ejecuta un SP SIN @PROCESO y devuelve DataSet
        protected async Task<DataSet> EjecutarSpDataSetSinProcesoAsync(string sp, IEnumerable<SqlParameter> parametros)
        {
            var ds = new DataSet();
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = sp;

                foreach (var p in parametros) cmd.Parameters.Add(p);

                await _context.Database.OpenConnectionAsync();
                using var da = new SqlDataAdapter((SqlCommand)cmd);
                await Task.Run(() => da.Fill(ds));
                return ds;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }












        /// Lee el primer entero (sirve para SCOPE_IDENTITY() o @@ROWCOUNT)
        protected static int ReadInt(DataSet ds, string colFallback = "Afectados")
        {
            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0) return 0;
            var row = ds.Tables[0].Rows[0];

            if (int.TryParse(row[0]?.ToString(), out var v0)) return v0;

            if (row.Table.Columns.Contains(colFallback) && int.TryParse(row[colFallback]?.ToString(), out var v1))
                return v1;

            foreach (DataColumn c in row.Table.Columns)
                if (int.TryParse(row[c]?.ToString(), out var v)) return v;

            return 0;
        }

        /// Intenta leer un ID por nombre de columna (por ejemplo "UsuarioID", "ClienteID"...)
        protected static int ReadIdentityByName(DataSet ds, params string[] idColumnCandidates)
        {
            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0) return 0;
            var row = ds.Tables[0].Rows[0];

            foreach (var name in idColumnCandidates)
                if (row.Table.Columns.Contains(name) && int.TryParse(row[name]?.ToString(), out var v))
                    return v;

            return ReadInt(ds);
        }

        /// Crea parámetros con DBNull cuando value es null
        protected static SqlParameter P(string name, SqlDbType type, object? value, int? size = null)
        {
            var p = size.HasValue ? new SqlParameter(name, type, size.Value) : new SqlParameter(name, type);
            p.Value = value ?? DBNull.Value;
            return p;
        }
    }
}
