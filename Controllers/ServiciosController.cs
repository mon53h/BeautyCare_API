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
    public class ServiciosController : ControllerBase
    {
        private readonly ServiciosRepository _repo;
        public ServiciosController(ServiciosRepository repo) => _repo = repo;

        // ===== DTO (ajusta los nombres a lo que devuelva tu SP) =====
        public class ServicioDto
        {
            public int ServicioID { get; set; }
            public string Nombre { get; set; } = "";
            public decimal Precio { get; set; }
            public int DuracionMin { get; set; } // o DuracionMinutos si así se llama en tu BD
        }

        // Helpers para leer columnas sin explotar si falta alguna
        private static T? GetFieldOrDefault<T>(DataRow r, string col)
        {
            return r.Table.Columns.Contains(col) && !r.IsNull(col) ? r.Field<T>(col) : default;
        }

        private static ServicioDto MapRow(DataRow r) => new ServicioDto
        {
            // OJO: estos nombres deben coincidir con lo que retorna tu SP/consulta
            ServicioID = GetFieldOrDefault<int>(r, "ServicioID"),
            Nombre = GetFieldOrDefault<string>(r, "Nombre") ?? "",
            Precio = GetFieldOrDefault<decimal>(r, "Precio"),
            DuracionMin = GetFieldOrDefault<int>(r, "DuracionMin") // cambia a "DuracionMinutos" si es tu columna real
        };

        private static List<ServicioDto> MapTable(DataTable? t)
            => t is null ? new List<ServicioDto>() : t.Rows.Cast<DataRow>().Select(MapRow).ToList();

        // 1) LISTA COMPLETA: GET /api/Servicios
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ServicioDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var ds = await _repo.ListarAsync(null, null);
            var lista = MapTable(ds.Tables.Count > 0 ? ds.Tables[0] : null);
            return Ok(lista);
        }

        // 2) DETALLE POR ID: GET /api/Servicios/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ServicioDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var ds = await _repo.ListarAsync(id, null);
            var t = ds.Tables.Count > 0 ? ds.Tables[0] : null;
            if (t is null || t.Rows.Count == 0) return NotFound();
            return Ok(MapRow(t.Rows[0]));
        }

        // 3) BÚSQUEDA POR NOMBRE (y/o id): GET /api/Servicios/search?nombre=...&servicioId=...
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<ServicioDto>), 200)]
        public async Task<IActionResult> Search(
            [FromQuery] int? servicioId = null,
            [FromQuery] string? nombre = null)
        {
            var ds = await _repo.ListarAsync(servicioId, nombre);
            var lista = MapTable(ds.Tables.Count > 0 ? ds.Tables[0] : null);
            return Ok(lista);
        }

        // 4) CREAR: POST /api/Servicios
        [HttpPost]
        public async Task<IActionResult> Post(
            [FromQuery] string nombre,
            [FromQuery] decimal precio,
            [FromQuery] int duracionMin)
        {
            var id = await _repo.CrearAsync(nombre, precio, duracionMin);
            return Ok(new { ServicioID = id });
        }

        // 5) ACTUALIZAR: PUT /api/Servicios/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(
            int id,
            [FromQuery] string? nombre = null,
            [FromQuery] decimal? precio = null,
            [FromQuery] int? duracionMin = null)
        {
            var afectados = await _repo.ActualizarAsync(id, nombre, precio, duracionMin);
            if (afectados <= 0) return NotFound();
            return Ok(new { Afectados = afectados });
        }

        // 6) ELIMINAR: DELETE /api/Servicios/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var afectados = await _repo.EliminarAsync(id);
            if (afectados <= 0) return NotFound();
            return Ok(new { Afectados = afectados });
        }
    }
}
