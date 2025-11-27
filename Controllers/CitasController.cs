using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BeautyCare_API.Repositorios;

namespace BeautyCare_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CitasController : ControllerBase
    {
        private readonly CitasRepository _repo;
        public CitasController(CitasRepository repo) => _repo = repo;

        // ===== DTOs =====
        public class CitaDto
        {
            public int CitaID { get; set; }
            public int ClienteID { get; set; }
            public int PersonalID { get; set; }
            public DateTime FechaHoraInicio { get; set; }
            public DateTime? FechaHoraFin { get; set; }
            public string? Estado { get; set; }
            public string? Descripcion { get; set; }
            public string? Notas { get; set; }
        }

        public class CitaCreateDTO
        {
            public int ClienteID { get; set; }
            public int PersonalID { get; set; }
            public DateTime FechaHoraInicio { get; set; }
            public DateTime? FechaHoraFin { get; set; }
            public string? Estado { get; set; }
            public string? Descripcion { get; set; }
            public string? Notas { get; set; }
            public List<int> ServicioIDs { get; set; } = new();
        }

        // ===== Helpers de mapeo seguros =====
        private static T? GetFieldOrDefault<T>(DataRow r, string name)
        {
            return r.Table.Columns.Contains(name) && !r.IsNull(name)
                ? r.Field<T>(name)
                : default;
        }

        private static CitaDto MapRow(DataRow r) => new CitaDto
        {
            // AJUSTA estos nombres a los que devuelve tu SP de Citas:
            CitaID = GetFieldOrDefault<int>(r, "CitaID"),
            ClienteID = GetFieldOrDefault<int>(r, "ClienteID"),
            PersonalID = GetFieldOrDefault<int>(r, "PersonalID"),
            FechaHoraInicio = GetFieldOrDefault<DateTime>(r, "FechaHoraInicio"),
            FechaHoraFin = GetFieldOrDefault<DateTime>(r, "FechaHoraFin"),
            Estado = GetFieldOrDefault<string>(r, "Estado"),
            Descripcion = GetFieldOrDefault<string>(r, "Descripcion"),
            Notas = GetFieldOrDefault<string>(r, "Notas")
        };

        private static List<CitaDto> MapTable(DataTable? t)
            => t is null ? new List<CitaDto>() : t.Rows.Cast<DataRow>().Select(MapRow).ToList();

        // ===== GETs =====

        // 1) LISTA COMPLETA: GET /api/Citas
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CitaDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var ds = await _repo.ListarAsync(null, null, null, null, null, null);
            var lista = MapTable(ds.Tables.Count > 0 ? ds.Tables[0] : null);
            return Ok(lista);
        }

        // 2) DETALLE POR ID: GET /api/Citas/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CitaDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var ds = await _repo.ListarAsync(id, null, null, null, null, null);
            var t = ds.Tables.Count > 0 ? ds.Tables[0] : null;
            if (t is null || t.Rows.Count == 0) return NotFound();
            return Ok(MapRow(t.Rows[0]));
        }

        // 3) BÚSQUEDA/FILTRO:
        // GET /api/Citas/search?citaId=&clienteId=&personalId=&estado=&desde=&hasta=
        // Fechas en ISO 8601: 2025-10-22T10:30:00
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<CitaDto>), 200)]
        public async Task<IActionResult> Search(
            [FromQuery] int? citaId = null,
            [FromQuery] int? clienteId = null,
            [FromQuery] int? personalId = null,
            [FromQuery] string? estado = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            var ds = await _repo.ListarAsync(citaId, clienteId, personalId, estado, desde, hasta);
            var lista = MapTable(ds.Tables.Count > 0 ? ds.Tables[0] : null);
            return Ok(lista);
        }

        // ===== COMANDOS =====

        // CREAR: POST /api/Citas  (body JSON)
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CitaCreateDTO dto)
        {
            if (dto == null) return BadRequest("Body requerido.");
            if (dto.ClienteID <= 0 || dto.PersonalID <= 0)
                return BadRequest("ClienteID y PersonalID son obligatorios.");
            if (dto.FechaHoraFin.HasValue && dto.FechaHoraFin < dto.FechaHoraInicio)
                return BadRequest("La FechaHoraFin no puede ser anterior a la FechaHoraInicio.");

            var id = await _repo.CrearAsync(
                dto.ClienteID, dto.PersonalID, dto.FechaHoraInicio, dto.FechaHoraFin,
                dto.Estado, dto.Descripcion, dto.Notas, dto.ServicioIDs
            );

            return Ok(new { CitaID = id });
        }

        // ACTUALIZAR: PUT /api/Citas/{id} (body JSON)
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] CitaCreateDTO dto)
        {
            if (dto == null) return BadRequest("Body requerido.");
            if (dto.FechaHoraFin.HasValue && dto.FechaHoraFin < dto.FechaHoraInicio)
                return BadRequest("La FechaHoraFin no puede ser anterior a la FechaHoraInicio.");

            var afectados = await _repo.ActualizarAsync(
                id, dto.ClienteID, dto.PersonalID, dto.FechaHoraInicio, dto.FechaHoraFin,
                dto.Estado, dto.Descripcion, dto.Notas, dto.ServicioIDs
            );

            if (afectados <= 0) return NotFound();
            return Ok(new { Afectados = afectados });
        }

        // ELIMINAR: DELETE /api/Citas/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var afectados = await _repo.EliminarAsync(id);
            if (afectados <= 0) return NotFound();
            return Ok(new { Afectados = afectados });
        }
    }
}
