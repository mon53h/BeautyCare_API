using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using BeautyCare_API.Repositorios;

namespace BeautyCare_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly LoginRepository _repo; // usa MAN_USUARIOS
        public UsuariosController(LoginRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? usuarioId = null, [FromQuery] string? nombreUsuario = null, [FromQuery] string? rol = null)
        {
            var ds = await _repo.ConsultarAsync(usuarioId, nombreUsuario, rol);
            return Ok(ds.Tables.Count > 0 ? ds.Tables[0] : null);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromQuery] string nombreUsuario, [FromQuery] string contrasenaHash, [FromQuery] string rol)
        {
            var id = await _repo.CrearAsync(nombreUsuario, contrasenaHash, rol);
            return Ok(new { UsuarioID = id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromQuery] string? nombreUsuario = null, [FromQuery] string? contrasenaHash = null, [FromQuery] string? rol = null)
        {
            var afectados = await _repo.ActualizarAsync(id, nombreUsuario, contrasenaHash, rol);
            if (afectados <= 0) return NotFound();
            return Ok(new { Afectados = afectados });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var afectados = await _repo.EliminarAsync(id);
            if (afectados <= 0) return NotFound();
            return Ok(new { Afectados = afectados });
        }
    }
}
