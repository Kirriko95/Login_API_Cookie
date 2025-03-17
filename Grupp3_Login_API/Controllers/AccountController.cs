using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Grupp3_Login_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Grupp3_Login_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ 1️⃣ Öppen endpoint: Registrera nytt användarkonto ("User")
        [HttpPost("register")]
        public async Task<ActionResult<Account>> RegisterUser([FromBody] Account account)
        {
            if (await _context.Accounts.AnyAsync(a => a.UserName == account.UserName))
            {
                return BadRequest("Användarnamnet är redan taget.");
            }

            account.Password = BCrypt.Net.BCrypt.HashPassword(account.Password);
            account.RoleId = 3; // "User"

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
        }

        // ✅ 2️⃣ Admin-endpoint: Skapa ett Employee-konto
        [HttpPost("create-employee")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Account>> CreateEmployee([FromBody] Account account)
        {
            if (await _context.Accounts.AnyAsync(a => a.UserName == account.UserName))
            {
                return BadRequest("Användarnamnet är redan taget.");
            }

            account.Password = BCrypt.Net.BCrypt.HashPassword(account.Password);
            account.RoleId = 2; // "Employee"

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
        }

        // ✅ 3️⃣ Hämta en användare baserat på Namn
        [HttpGet("user/{userName}")]
        [Authorize] 
        public async Task<ActionResult<Account>> GetAccount(string userName)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserName == userName);

            if (account == null)
            {
                return NotFound("Kontot hittades inte.");
            }

            return Ok(account);
        }

        // ✅ 4️⃣ Hämta alla konton (Endast Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Account>>> GetAllAccounts()
        {
            return await _context.Accounts.ToListAsync();
        }

        // ✅ 5️⃣ Admin: Uppdatera konto
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] Account updatedAccount)
        {
            if (id != updatedAccount.Id)
            {
                return BadRequest("ID:t i URL:en matchar inte kontots ID.");
            }

            var existingAccount = await _context.Accounts.FindAsync(id);
            if (existingAccount == null)
            {
                return NotFound("Kontot hittades inte.");
            }

            // Uppdatera fält (exempel: användarnamn och roll)
            existingAccount.UserName = updatedAccount.UserName;
            existingAccount.RoleId = updatedAccount.RoleId;

            // Uppdatera lösenord om det skickas med
            if (!string.IsNullOrWhiteSpace(updatedAccount.Password))
            {
                existingAccount.Password = BCrypt.Net.BCrypt.HashPassword(updatedAccount.Password);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "Ett fel uppstod vid uppdatering av kontot.");
            }

            return NoContent();
        }

        // ✅ 6️⃣ Admin: Ta bort ett konto
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
            {
                return NotFound("Kontot hittades inte.");
            }

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
