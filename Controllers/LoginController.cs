using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using BeautyCare_API.Repositorios;


namespace BeautyCare_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly LoginRepository _repo;
        private readonly IConfiguration _config;

        public LoginController(LoginRepository repo, IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }

        public record LoginRequest(string NombreUsuario, string Contrasena);

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.NombreUsuario) || string.IsNullOrWhiteSpace(dto.Contrasena))
                return BadRequest("Credenciales incompletas.");

            var ds = await _repo.AutenticarAsync(dto.NombreUsuario, dto.Contrasena);
            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                return Unauthorized("Usuario o contraseña incorrectos.");

            var row = ds.Tables[0].Rows[0];
            var usuarioId = int.Parse(row["UsuarioID"].ToString()!);
            var user = row["NombreUsuario"].ToString()!;
            var rol = row["Rol"].ToString()!;

            var token = GenerateToken(usuarioId, user, rol);
            return Ok(new { UsuarioID = usuarioId, NombreUsuario = user, Rol = rol, Token = token });
        }

        private string GenerateToken(int usuarioId, string nombreUsuario, string rol)
        {
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, nombreUsuario),
                new Claim(ClaimTypes.Role, rol)
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.TryParse(jwt["ExpiresMinutes"], out var m) ? m : 120),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
