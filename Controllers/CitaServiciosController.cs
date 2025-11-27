using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BeautyCare_API.Repositorios;

namespace BeautyCare_API.Controllers
{
    [ApiController]
    [Route("api/citas/{citaId:int}/servicios")]
    [Authorize]
    [Produces("application/json")]
    public class CitaServiciosController : ControllerBase
    {
        private readonly CitasServiciosRepository _repo;
        public CitaServiciosController(CitasServiciosRepository repo) => _repo = repo;

        // DTO para listado detallado (SP 91)
        public class CitaServicioListItem
        {
            public int CitaID { get; set; }
            public int ServicioID { get; set; }
            public string Nombre { get; set; } = "";
            public int Cantidad { get; set; }
            public decimal? PrecioUnitario { get; set; }
            public decimal TotalLinea { get; set; }
        }

        // Body para POST (opcional cantidad/precio)
        public class AddServicioBody
        {
            public int ServicioID { get; set; }
            public int? Cantidad { get; set; }
            public decimal? PrecioUnitario { get; set; }
        }

        private static bool Has(DataTable t, string col) => t.Columns.Contains(col);

        // A) LISTA GENERAL — SP 90
        // GET /api/citas-servicios?citaId=&servicioId=
        [HttpGet("~/api/citas-servicios")]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] int? citaId = null, [FromQuery] int? servicioId = null)
        {
            var ds = await _repo.ListarAsync(citaId, servicioId, detalle: false);
            var t = ds.Tables.Count > 0 ? ds.Tables[0] : null;

            var list = new List<object>();
            if (t != null)
            {
                foreach (DataRow r in t.Rows)
                {
                    list.Add(new
                    {
                        CitaID = Convert.ToInt32(r["CitaID"]),
                        ServicioID = Convert.ToInt32(r["ServicioID"]),
                        Cantidad = Has(t, "Cantidad") && r["Cantidad"] != DBNull.Value ? (int?)Convert.ToInt32(r["Cantidad"]) : null,
                        PrecioUnitario = Has(t, "PrecioUnitario") && r["PrecioUnitario"] != DBNull.Value ? (decimal?)Convert.ToDecimal(r["PrecioUnitario"]) : null
                    });
                }
            }
            return Ok(list);
        }

        // B) LISTA POR CITA — SP 91 (bonito con Nombre y TotalLinea)
        // GET /api/citas/{citaId}/servicios?servicioId=
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CitaServicioListItem>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByCita([FromRoute] int citaId, [FromQuery] int? servicioId = null)
        {
            var ds = await _repo.ListarAsync(citaId, servicioId, detalle: true);
            var t = ds.Tables.Count > 0 ? ds.Tables[0] : null;

            var list = new List<CitaServicioListItem>();
            if (t != null)
            {
                foreach (DataRow r in t.Rows)
                {
                    list.Add(new CitaServicioListItem
                    {
                        CitaID = Convert.ToInt32(r["CitaID"]),
                        ServicioID = Convert.ToInt32(r["ServicioID"]),
                        Nombre = Has(t, "Nombre") && r["Nombre"] != DBNull.Value ? (string)r["Nombre"] : "",
                        Cantidad = Has(t, "Cantidad") && r["Cantidad"] != DBNull.Value ? Convert.ToInt32(r["Cantidad"]) : 1,
                        PrecioUnitario = Has(t, "PrecioUnitario") && r["PrecioUnitario"] != DBNull.Value ? Convert.ToDecimal(r["PrecioUnitario"]) : (decimal?)null,
                        TotalLinea = Has(t, "TotalLinea") && r["TotalLinea"] != DBNull.Value ? Convert.ToDecimal(r["TotalLinea"]) : 0m
                    });
                }
            }
            return Ok(list);
        }

        // C) TOTAL POR CITA — SP 92
        // GET /api/citas/{citaId}/total
        [HttpGet("~/api/citas/{citaId:int}/total")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTotal([FromRoute] int citaId)
        {
            var ds = await _repo.TotalPorCitaAsync(citaId);
            var t = ds.Tables.Count > 0 ? ds.Tables[0] : null;

            var total = 0m;
            if (t != null && t.Rows.Count > 0 && t.Columns.Contains("TotalCita") && t.Rows[0]["TotalCita"] != DBNull.Value)
                total = Convert.ToDecimal(t.Rows[0]["TotalCita"]);

            return Ok(new { CitaID = citaId, TotalCita = total });
        }

        // D) POST — inserta vínculo (opcional cantidad/precio)
        // POST /api/citas/{citaId}/servicios
        [HttpPost]
        public async Task<IActionResult> PostBody([FromRoute] int citaId, [FromBody] AddServicioBody body)
        {
            if (body is null || body.ServicioID <= 0)
                return BadRequest("ServicioID es requerido y debe ser > 0.");

            var res = await _repo.InsertarAsync(citaId, body.ServicioID, body.Cantidad, body.PrecioUnitario);
            return Ok(new { Resultado = res }); // 1=Insertado, 2=Actualizada Cantidad
        }

        // E) POST (ruta corta) — inserta sin body
        // POST /api/citas/{citaId}/servicios/{servicioId}
        [HttpPost("{servicioId:int}")]
        public async Task<IActionResult> PostRoute([FromRoute] int citaId, [FromRoute] int servicioId)
        {
            if (servicioId <= 0) return BadRequest("ServicioID inválido.");
            var res = await _repo.InsertarAsync(citaId, servicioId);
            return Ok(new { Resultado = res });
        }

        // F) PUT BULK — reemplaza todos los servicios de la cita
        // PUT /api/citas/{citaId}/servicios/bulk
        [HttpPut("bulk")]
        public async Task<IActionResult> PutBulk([FromRoute] int citaId, [FromBody] List<int> servicioIds)
        {
            if (servicioIds is null) return BadRequest("Debe enviar la lista de servicios.");
            if (servicioIds.Count == 0) return BadRequest("La lista de servicios no puede estar vacía.");
            foreach (var id in servicioIds) if (id <= 0) return BadRequest("Todos los ServicioID deben ser > 0.");

            var insertados = await _repo.ReemplazarTodoAsync(citaId, servicioIds);
            return Ok(new { Insertados = insertados });
        }

        // G) DELETE — elimina vínculo
        // DELETE /api/citas/{citaId}/servicios/{servicioId}
        [HttpDelete("{servicioId:int}")]
        public async Task<IActionResult> Delete([FromRoute] int citaId, [FromRoute] int servicioId)
        {
            if (servicioId <= 0) return BadRequest("ServicioID inválido.");
            var afect = await _repo.EliminarAsync(citaId, servicioId);
            if (afect <= 0) return NotFound();
            return Ok(new { Afectados = afect });
        }
    }
}
