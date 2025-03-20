using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Grupp3_Login_API.Models;
using System.Threading.Tasks;
using BCrypt.Net;
using System.Security.Claims;

namespace Grupp3_Login_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthenticationController(AppDbContext context)
        {
            _context = context;
        }

        // Inloggning: Skapar en cookie
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

            // Skapa en lista med claims
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User") // Standardroll: User
            };


            // Skapa identity och autentisering cookie
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                // Se till att cookien skickas till andra domäner
                IsPersistent = true, // För att cookien ska vara kvar vid efterföljande begärningar
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // Cookie-utgångstid
            };

            // Logga in och skapa cookie
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);

            return Ok(new { Message = "Inloggad framgångsrikt.", Role = user.Role?.RoleName ?? "User" });

        }

        // Logout-metod för att ta bort cookie
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new { Message = "Utloggning lyckades." });
        }
    }
}
