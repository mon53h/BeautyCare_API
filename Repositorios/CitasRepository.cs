using BeautyCare_API.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BeautyCare_API.Repositorios
{
    public class CitasRepository : BaseSpRepository
    {
        private const string SP_CITA = "MAN_CITAS";
        private const string SP_DET = "MAN_CITA_SERVICIOS";

        public CitasRepository(AplicationsDbContext db) : base(db) { }

        // ⚠️ AJUSTA esta lista para que coincida EXACTO con tu CHECK CK_Citas_Estado
        private static readonly string[] ESTADOS_PERMITIDOS =
        {
            "Agendada", "Pendiente", "Completada", "Cancelada"
            // añade aquí otros si tu CHECK lo permite
        };

        private static string NormalizarEstado(string? estado)
        {
            var v = (estado ?? "").Trim();
            if (string.IsNullOrEmpty(v)) return "Agendada";
            return ESTADOS_PERMITIDOS.Any(e => string.Equals(e, v, StringComparison.OrdinalIgnoreCase))
                ? v
                : "Agendada";
        }

        public Task<DataSet> ListarAsync(
            int? citaId = null, int? clienteId = null, int? personalId = null,
            string? estado = null, DateTime? desde = null, DateTime? hasta = null)
            => EjecutarSpDataSetAsync(SP_CITA, 90, new[]
            {
                P("@CitaID",     SqlDbType.Int,      citaId),
                P("@ClienteID",  SqlDbType.Int,      clienteId),
                P("@PersonalID", SqlDbType.Int,      personalId),
                P("@Estado",     SqlDbType.VarChar,  estado, 20),
                P("@Desde",      SqlDbType.DateTime, desde),
                P("@Hasta",      SqlDbType.DateTime, hasta)
            });

        public async Task<int> CrearAsync(
            int clienteId, int personalId, DateTime inicio, DateTime? fin,
            string? estado, string? descripcion, string? notas,
            IEnumerable<int> servicioIds)
        {
            await _context.Database.OpenConnectionAsync();
            var conn = (SqlConnection)_context.Database.GetDbConnection();
            await using var trx = await conn.BeginTransactionAsync();

            try
            {
                // 1) CABECERA (INS=1)
                DataSet dsCab;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = (SqlTransaction)trx;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = SP_CITA;

                    var estadoNorm = NormalizarEstado(estado);

                    cmd.Parameters.Add(new SqlParameter("@PROCESO", SqlDbType.TinyInt) { Value = 1 });
                    cmd.Parameters.Add(P("@ClienteID", SqlDbType.Int, clienteId));
                    cmd.Parameters.Add(P("@PersonalID", SqlDbType.Int, personalId));
                    cmd.Parameters.Add(P("@FechaHoraInicio", SqlDbType.DateTime, inicio));
                    cmd.Parameters.Add(P("@FechaHoraFin", SqlDbType.DateTime, fin));
                    cmd.Parameters.Add(P("@Estado", SqlDbType.VarChar, estadoNorm, 20));
                    cmd.Parameters.Add(P("@Descripcion", SqlDbType.VarChar, descripcion));
                    cmd.Parameters.Add(P("@Notas", SqlDbType.VarChar, notas));

                    using var da = new SqlDataAdapter((SqlCommand)cmd);
                    dsCab = new DataSet();
                    await Task.Run(() => da.Fill(dsCab));
                }

                var citaId = ReadIdentityByName(dsCab, "CitaID");
                if (citaId <= 0) throw new Exception("No se obtuvo CitaID al crear la cabecera.");

                // 2) DETALLE (INS=1 por servicio)
                foreach (var sid in (servicioIds ?? Enumerable.Empty<int>()))
                {
                    using var cmdDet = conn.CreateCommand();
                    cmdDet.Transaction = (SqlTransaction)trx;
                    cmdDet.CommandType = CommandType.StoredProcedure;
                    cmdDet.CommandText = SP_DET;

                    cmdDet.Parameters.Add(new SqlParameter("@PROCESO", SqlDbType.TinyInt) { Value = 1 });
                    cmdDet.Parameters.Add(P("@CitaID", SqlDbType.Int, citaId));
                    cmdDet.Parameters.Add(P("@ServicioID", SqlDbType.Int, sid));

                    await cmdDet.ExecuteNonQueryAsync();
                }

                await trx.CommitAsync();
                return citaId;
            }
            catch
            {
                await trx.RollbackAsync();
                throw;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        public async Task<int> ActualizarAsync(
            int citaId, int clienteId, int personalId, DateTime inicio, DateTime? fin,
            string? estado, string? descripcion, string? notas,
            IEnumerable<int> servicioIds)
        {
            await _context.Database.OpenConnectionAsync();
            var conn = (SqlConnection)_context.Database.GetDbConnection();
            await using var trx = await conn.BeginTransactionAsync();

            try
            {
                // 1) UPDATE CABECERA (UPD=2)
                DataSet dsUpd;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = (SqlTransaction)trx;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = SP_CITA;

                    var estadoNorm = NormalizarEstado(estado);

                    cmd.Parameters.Add(new SqlParameter("@PROCESO", SqlDbType.TinyInt) { Value = 2 });
                    cmd.Parameters.Add(P("@CitaID", SqlDbType.Int, citaId));
                    cmd.Parameters.Add(P("@ClienteID", SqlDbType.Int, clienteId));
                    cmd.Parameters.Add(P("@PersonalID", SqlDbType.Int, personalId));
                    cmd.Parameters.Add(P("@FechaHoraInicio", SqlDbType.DateTime, inicio));
                    cmd.Parameters.Add(P("@FechaHoraFin", SqlDbType.DateTime, fin));
                    cmd.Parameters.Add(P("@Estado", SqlDbType.VarChar, estadoNorm, 20));
                    cmd.Parameters.Add(P("@Descripcion", SqlDbType.VarChar, descripcion));
                    cmd.Parameters.Add(P("@Notas", SqlDbType.VarChar, notas));

                    using var da = new SqlDataAdapter((SqlCommand)cmd);
                    dsUpd = new DataSet();
                    await Task.Run(() => da.Fill(dsUpd));
                }

                var afectadosCab = ReadInt(dsUpd, "Afectados");
                if (afectadosCab == 0) throw new Exception("No se actualizó la cabecera de la cita.");

                // 2) LEE DETALLE ACTUAL
                var dsDetActual = await EjecutarSpDataSetAsync(SP_DET, 90, new[]
                {
                    P("@CitaID",     SqlDbType.Int, citaId),
                    P("@ServicioID", SqlDbType.Int, DBNull.Value)
                });

                // 3) BORRA DETALLE ACTUAL (DEL=3)
                if (dsDetActual.Tables.Count > 0)
                {
                    foreach (DataRow r in dsDetActual.Tables[0].Rows)
                    {
                        var sid = Convert.ToInt32(r["ServicioID"]);
                        using var cmdDel = conn.CreateCommand();
                        cmdDel.Transaction = (SqlTransaction)trx;
                        cmdDel.CommandType = CommandType.StoredProcedure;
                        cmdDel.CommandText = SP_DET;

                        cmdDel.Parameters.Add(new SqlParameter("@PROCESO", SqlDbType.TinyInt) { Value = 3 });
                        cmdDel.Parameters.Add(P("@CitaID", SqlDbType.Int, citaId));
                        cmdDel.Parameters.Add(P("@ServicioID", SqlDbType.Int, sid));

                        await cmdDel.ExecuteNonQueryAsync();
                    }
                }

                // 4) INSERTA NUEVO DETALLE
                foreach (var sid in (servicioIds ?? Enumerable.Empty<int>()))
                {
                    using var cmdIns = conn.CreateCommand();
                    cmdIns.Transaction = (SqlTransaction)trx;
                    cmdIns.CommandType = CommandType.StoredProcedure;
                    cmdIns.CommandText = SP_DET;

                    cmdIns.Parameters.Add(new SqlParameter("@PROCESO", SqlDbType.TinyInt) { Value = 1 });
                    cmdIns.Parameters.Add(P("@CitaID", SqlDbType.Int, citaId));
                    cmdIns.Parameters.Add(P("@ServicioID", SqlDbType.Int, sid));

                    await cmdIns.ExecuteNonQueryAsync();
                }

                await trx.CommitAsync();
                return afectadosCab;
            }
            catch
            {
                await trx.RollbackAsync();
                throw;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        public async Task<int> EliminarAsync(int citaId)
        {
            await _context.Database.OpenConnectionAsync();
            var conn = (SqlConnection)_context.Database.GetDbConnection();
            await using var trx = await conn.BeginTransactionAsync();

            try
            {
                // 1) BORRAR DETALLE
                var dsDet = await EjecutarSpDataSetAsync(SP_DET, 90, new[]
                {
                    P("@CitaID",     SqlDbType.Int, citaId),
                    P("@ServicioID", SqlDbType.Int, DBNull.Value)
                });

                if (dsDet.Tables.Count > 0)
                {
                    foreach (DataRow r in dsDet.Tables[0].Rows)
                    {
                        var sid = Convert.ToInt32(r["ServicioID"]);
                        using var cmdDel = conn.CreateCommand();
                        cmdDel.Transaction = (SqlTransaction)trx;
                        cmdDel.CommandType = CommandType.StoredProcedure;
                        cmdDel.CommandText = SP_DET;

                        cmdDel.Parameters.Add(new SqlParameter("@PROCESO", SqlDbType.TinyInt) { Value = 3 });
                        cmdDel.Parameters.Add(P("@CitaID", SqlDbType.Int, citaId));
                        cmdDel.Parameters.Add(P("@ServicioID", SqlDbType.Int, sid));

                        await cmdDel.ExecuteNonQueryAsync();
                    }
                }

                // 2) BORRAR CABECERA
                DataSet dsDelCab;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = (SqlTransaction)trx;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = SP_CITA;

                    cmd.Parameters.Add(new SqlParameter("@PROCESO", SqlDbType.TinyInt) { Value = 3 });
                    cmd.Parameters.Add(P("@CitaID", SqlDbType.Int, citaId));

                    using var da = new SqlDataAdapter((SqlCommand)cmd);
                    dsDelCab = new DataSet();
                    await Task.Run(() => da.Fill(dsDelCab));
                }

                var afectados = ReadInt(dsDelCab, "Afectados");
                await trx.CommitAsync();
                return afectados;
            }
            catch
            {
                await trx.RollbackAsync();
                throw;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }
}
