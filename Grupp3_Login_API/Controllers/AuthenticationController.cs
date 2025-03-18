using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Grupp3_Login_API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Grupp3_Login_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _jwtSecret;

        public AuthenticationController(AppDbContext context)
        {
            _context = context;
            _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            if (string.IsNullOrEmpty(_jwtSecret))
            {
                Console.WriteLine("JWT_SECRET_KEY saknas! Kontrollera miljövariabler.");
                throw new InvalidOperationException("JWT_SECRET_KEY is missing from environment variables.");
            }
        }

        // ✅ Inloggning: Genererar en JWT-token med användarens ID och roll
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.UserName == request.UserName);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return Unauthorized("Felaktigt användarnamn eller lösenord.");
            }

            // Konvertera roll-ID till läsbar text
            string roleName = user.Role?.RoleName ?? "User"; // Om ingen roll finns, sätt "User" som standard

            // Skapa JWT-token
            var key = Encoding.UTF8.GetBytes(_jwtSecret);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ✅ Lägger till user.Id
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Role, roleName) // ✅ Roller är nu läsbara istället för siffror
                }),
                Expires = DateTime.UtcNow.AddMinutes(60), // ✅ Konfigurerbar livslängd
                Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "https://localhost:7200/",
                Audience = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "https://localhost:7200",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { Token = tokenString, UserId = user.Id, Role = roleName });
        }
    }
}

