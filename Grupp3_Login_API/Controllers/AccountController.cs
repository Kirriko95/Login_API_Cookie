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

        // Registrera ett nytt användarkonto (Får automatiskt rollen "User")
        [HttpPost("register")]
        public async Task<ActionResult<Account>> RegisterUser([FromBody] Account account)
        {
            if (await _context.Accounts.AnyAsync(a => a.UserName == account.UserName))
            {
                return BadRequest("Användarnamnet är redan taget.");
            }

            account.Password = BCrypt.Net.BCrypt.HashPassword(account.Password);
            account.RoleId = 3; // Standardroll: "User"

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAllAccounts), new { id = account.Id }, account);
        }

        // Admin kan skapa ett Employee-konto (Får automatiskt rollen "Employee")
        [HttpPost("create-employee")]
        [Authorize(Roles = "Admin")] // Endast admin kan skapa employee-konton
        public async Task<ActionResult<Account>> CreateEmployee([FromBody] Account account)
        {
            if (await _context.Accounts.AnyAsync(a => a.UserName == account.UserName))
            {
                return BadRequest("Användarnamnet är redan taget.");
            }

            account.Password = BCrypt.Net.BCrypt.HashPassword(account.Password);
            account.RoleId = 2; // Standardroll: "Employee"

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAllAccounts), new { id = account.Id }, account);
        }

        // Hämta alla konton (Endast Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")] // Endast admin får hämta alla konton
        public async Task<ActionResult<IEnumerable<Account>>> GetAllAccounts()
        {
            return await _context.Accounts.ToListAsync();
        }

        // Admin: Uppdatera konto (Kräver att admin anger nuvarande lösenord)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Endast admin kan uppdatera konto
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] UpdateAccountRequest request)
        {
            var existingAccount = await _context.Accounts.FindAsync(id);
            if (existingAccount == null)
            {
                return NotFound("Kontot hittades inte.");
            }

            // Om lösenord ska uppdateras, verifiera nuvarande lösenord
            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword) || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, existingAccount.Password))
                {
                    return BadRequest("Felaktigt nuvarande lösenord.");
                }
                existingAccount.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }

            // Uppdatera användarnamn om det skickas med
            if (!string.IsNullOrWhiteSpace(request.NewUserName) && existingAccount.UserName != request.NewUserName)
            {
                existingAccount.UserName = request.NewUserName;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Admin: Ta bort ett konto
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Endast admin kan ta bort konto
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
