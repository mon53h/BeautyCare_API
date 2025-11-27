using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using BeautyCare_API.Data;

namespace BeautyCare_API.Repositorios
{
    public class CitasServiciosRepository : BaseSpRepository
    {
        private const string SP = "MAN_CITA_SERVICIOS";

        public CitasServiciosRepository(AplicationsDbContext db) : base(db) { }

        // A) Lista: si pides por CitaID y detalle=true -> PROCESO=91 (JOIN Servicios)
        //    en otros casos -> PROCESO=90 (básico)
        public System.Threading.Tasks.Task<DataSet> ListarAsync(int? citaId = null, int? servicioId = null, bool detalle = false)
        {
            var proceso = (detalle && citaId.HasValue) ? 91 : 90;
            return EjecutarSpDataSetAsync(SP, proceso, new[]
            {
                P("@CitaID",     SqlDbType.Int,  citaId.HasValue ? (object)citaId.Value : DBNull.Value),
                P("@ServicioID", SqlDbType.Int,  servicioId.HasValue ? (object)servicioId.Value : DBNull.Value)
            });
        }

        // B) Total por cita (PROCESO=92)
        public System.Threading.Tasks.Task<DataSet> TotalPorCitaAsync(int citaId)
        {
            return EjecutarSpDataSetAsync(SP, 92, new[]
            {
                P("@CitaID", SqlDbType.Int, citaId)
            });
        }

        // C) Insertar (PROCESO=1) — SP devuelve "Resultado": 1=Insertado, 2=Actualizada Cantidad
        public async System.Threading.Tasks.Task<int> InsertarAsync(int citaId, int servicioId, int? cantidad = null, decimal? precioUnitario = null)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 1, new[]
            {
                P("@CitaID",         SqlDbType.Int,     citaId),
                P("@ServicioID",     SqlDbType.Int,     servicioId),
                P("@Cantidad",       SqlDbType.Int,     cantidad.HasValue ? (object)cantidad.Value : DBNull.Value),
                P("@PrecioUnitario", SqlDbType.Decimal, precioUnitario.HasValue ? (object)precioUnitario.Value : DBNull.Value)
            });

            var val = ReadInt(ds, "Resultado");
            return val != 0 ? val : ReadInt(ds, "Insertado"); // compat con versión previa
        }

        // D) Eliminar (PROCESO=3)
        public async System.Threading.Tasks.Task<int> EliminarAsync(int citaId, int servicioId)
        {
            var ds = await EjecutarSpDataSetAsync(SP, 3, new[]
            {
                P("@CitaID",     SqlDbType.Int, citaId),
                P("@ServicioID", SqlDbType.Int, servicioId)
            });
            return ReadInt(ds, "Afectados");
        }

        // E) Reemplazar todos los servicios de una cita (borra actuales e inserta nuevos)
        public async System.Threading.Tasks.Task<int> ReemplazarTodoAsync(int citaId, IEnumerable<int> servicioIds)
        {
            var dsActual = await ListarAsync(citaId, null, detalle: false);

            // Borrar actuales
            if (dsActual.Tables.Count > 0)
            {
                var t = dsActual.Tables[0];
                foreach (DataRow r in t.Rows)
                {
                    int sid = Convert.ToInt32(r["ServicioID"]);
                    await EliminarAsync(citaId, sid);
                }
            }

            // Insertar nuevos (evitar duplicados)
            var vistos = new HashSet<int>();
            int insertados = 0;
            foreach (var sid in servicioIds)
            {
                if (sid <= 0) continue;
                if (vistos.Add(sid))
                    insertados += await InsertarAsync(citaId, sid);
            }
            return insertados;
        }
    }
}
