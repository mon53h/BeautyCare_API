using System;
using System.Linq;
using System.Data;
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
    public class ClientesController : ControllerBase
    {
        private readonly ClientesRepository _repo;
        public ClientesController(ClientesRepository repo) => _repo = repo;

        // ===== DTO =====
        public class ClienteDto
        {
            public int ClienteID { get; set; }
            public string Nombre { get; set; } = "";
            public string? Apellidos { get; set; }
            public string? Telefono { get; set; }
            public string CorreoElectronico { get; set; } = "";
            public DateTime FechaRegistro { get; set; }
        }

        // ===== Helpers =====
        private static T? GetFieldOrDefault<T>(DataRow r, string name)
        {
            return r.Table.Columns.Contains(name) && !r.IsNull(name)
                ? r.Field<T>(name)
                : default;
        }

        private static ClienteDto MapRow(DataRow r) => new ClienteDto
        {
            // Nombres EXACTOS según tu SP MAN_CLIENTES
            ClienteID = GetFieldOrDefault<int>(r, "ClienteID"),
            Nombre = GetFieldOrDefault<string>(r, "Nombre") ?? "",
            Apellidos = GetFieldOrDefault<string>(r, "Apellidos"),
            Telefono = GetFieldOrDefault<string>(r, "Telefono"),
            CorreoElectronico = GetFieldOrDefault<string>(r, "CorreoElectronico") ?? "",
            FechaRegistro = GetFieldOrDefault<DateTime>(r, "FechaRegistro")
        };

        private static List<ClienteDto> MapTable(DataTable? t)
            => t is null ? new List<ClienteDto>() : t.Rows.Cast<DataRow>().Select(MapRow).ToList();

        // 1) LISTA COMPLETA (sin parámetros): GET /api/Clientes
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ClienteDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var ds = await _repo.ListarAsync(null, null, null, null); // @PROCESO=90 con filtros NULL
            var lista = MapTable(ds.Tables.Count > 0 ? ds.Tables[0] : null);
            return Ok(lista);
        }

        // 2) OBTENER POR ID: GET /api/Clientes/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ClienteDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var ds = await _repo.ListarAsync(id, null, null, null);
            var t = ds.Tables.Count > 0 ? ds.Tables[0] : null;
            if (t is null || t.Rows.Count == 0) return NotFound();

            var cli = MapRow(t.Rows[0]);
            return Ok(cli);
        }

        // 3) BÚSQUEDA (filtros exactos como tu SP): GET /api/Clientes/search?nombre=...&apellidos=...&correoElectronico=...
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<ClienteDto>), 200)]
        public async Task<IActionResult> Search(
            [FromQuery] int? clienteId = null,
            [FromQuery] string? nombre = null,
            [FromQuery] string? apellidos = null,
            [FromQuery(Name = "correoElectronico")] string? correoElectronico = null)
        {
            var ds = await _repo.ListarAsync(clienteId, nombre, apellidos, correoElectronico);
            var lista = MapTable(ds.Tables.Count > 0 ? ds.Tables[0] : null);
            return Ok(lista);
        }

        // 4) CREAR: POST /api/Clientes (query params, alineado con SP)
        [HttpPost]
        public async Task<IActionResult> Post(
            [FromQuery] string nombre,
            [FromQuery] string? apellidos,
            [FromQuery] string? telefono,
            [FromQuery(Name = "correoElectronico")] string? correoElectronico,
            [FromQuery] DateTime? fechaRegistro = null)
        {
            var id = await _repo.CrearAsync(nombre, apellidos, telefono, correoElectronico, fechaRegistro);
            return Ok(new { ClienteID = id });
        }

        // 5) ACTUALIZAR: PUT /api/Clientes/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(
            int id,
            [FromQuery] string? nombre = null,
            [FromQuery] string? apellidos = null,
            [FromQuery] string? telefono = null,
            [FromQuery(Name = "correoElectronico")] string? correoElectronico = null)
        {
            var afectados = await _repo.ActualizarAsync(id, nombre, apellidos, telefono, correoElectronico);
            if (afectados <= 0) return NotFound();
            return Ok(new { Afectados = afectados });
        }

        // 6) ELIMINAR: DELETE /api/Clientes/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var afectados = await _repo.EliminarAsync(id);
            if (afectados <= 0) return NotFound();
            return Ok(new { Afectados = afectados });
        }
    }
}
