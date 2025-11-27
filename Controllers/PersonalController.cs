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
    public class PersonalController : ControllerBase
    {
        private readonly PersonalRepository _repo;
        public PersonalController(PersonalRepository repo) => _repo = repo;

        // DTO expuesto por la API
        public class PersonalDto
        {
            public int PersonalID { get; set; }
            public string Nombre { get; set; } = "";
            public string? Apellido { get; set; }            // o Apellidos
            public string Rol { get; set; } = "";
            public string? Tel { get; set; }                  // ← queremos exponer tel
            public string? Correo { get; set; }               // ← y correo
            public DateTime? FechaIngreso { get; set; }
            public bool? Activo { get; set; }
        }

        // ===== Helpers =====

        private static T? GetFieldOrDefault<T>(DataRow r, string name)
        {
            return r.Table.Columns.Contains(name) && !r.IsNull(name)
                ? r.Field<T>(name)
                : default;
        }

        // lee string probando varios nombres de columna
        private static string? GetStringFrom(DataRow r, params string[] names)
        {
            foreach (var n in names)
            {
                if (r.Table.Columns.Contains(n) && !r.IsNull(n))
                    return r[n]?.ToString();
            }
            return null;
        }

        private static PersonalDto MapRow(DataRow r) => new PersonalDto
        {
            PersonalID = GetFieldOrDefault<int>(r, "PersonalID"),
            Nombre = GetStringFrom(r, "Nombre") ?? "",
            Apellido = GetStringFrom(r, "Apellido", "Apellidos"),
            Rol = GetStringFrom(r, "Rol") ?? "",
            // 👇 aquí el cambio importante: buscamos Tel/Telefono y Correo/CorreoElectronico
            Tel = GetStringFrom(r, "Tel", "Telefono") ?? "",
            Correo = GetStringFrom(r, "Correo", "CorreoElectronico") ?? "",
            FechaIngreso = GetFieldOrDefault<DateTime>(r, "FechaIngreso"),
            Activo = GetFieldOrDefault<bool>(r, "Activo")
        };

        private static List<PersonalDto> MapTable(DataTable? t)
            => t is null ? new List<PersonalDto>() : t.Rows.Cast<DataRow>().Select(MapRow).ToList();

        // GET /api/Personal
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PersonalDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var ds = await _repo.ListarAsync(null, null, null);
            var lista = MapTable(ds.Tables.Count > 0 ? ds.Tables[0] : null);
            return Ok(lista);
        }

        // GET /api/Personal/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PersonalDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var ds = await _repo.ListarAsync(id, null, null);
            var t = ds.Tables.Count > 0 ? ds.Tables[0] : null;
            if (t is null || t.Rows.Count == 0) return NotFound();
            return Ok(MapRow(t.Rows[0]));
        }

        // GET /api/Personal/search?personalId=&rol=&activo=
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<PersonalDto>), 200)]
        public async Task<IActionResult> Search(
            [FromQuery] int? personalId = null,
            [FromQuery] string? rol = null,
            [FromQuery] bool? activo = null)
        {
            var ds = await _repo.ListarAsync(personalId, rol, activo);
            var lista = MapTable(ds.Tables.Count > 0 ? ds.Tables[0] : null);
            return Ok(lista);
        }

        // POST /api/Personal  (nota: FromBody sería más típico)
        [HttpPost]
        public async Task<IActionResult> Post(
            [FromQuery] string nombre,
            [FromQuery] string rol,
            [FromQuery] string? apellido = null,
            [FromQuery] string? tel = null,
            [FromQuery] string? correo = null,
            [FromQuery] DateTime? fechaIngreso = null,
            [FromQuery] bool? activo = true)
        {
            var id = await _repo.CrearAsync(nombre, apellido, rol, tel, correo, fechaIngreso, activo);
            return Ok(new { PersonalID = id });
        }

        // PUT /api/Personal/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(
            int id,
            [FromQuery] string? nombre = null,
            [FromQuery] string? apellido = null,
            [FromQuery] string? rol = null,
            [FromQuery] string? tel = null,
            [FromQuery] string? correo = null,
            [FromQuery] DateTime? fechaIngreso = null,
            [FromQuery] bool? activo = null)
        {
            var afectados = await _repo.ActualizarAsync(id, nombre, apellido, rol, tel, correo, fechaIngreso, activo);
            if (afectados <= 0) return NotFound();
            return Ok(new { Afectados = afectados });
        }

        // DELETE /api/Personal/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var afectados = await _repo.EliminarAsync(id);
            if (afectados <= 0) return NotFound();
            return Ok(new { Afectados = afectados });
        }
    }
}
