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

            account.Role = await _context.Roles.FindAsync(3);

            if (account.Role == null)
            {
                return BadRequest("Rollen 'User' existerar inte i databasen");
            }

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

            account.Role = await _context.Roles.FindAsync(2);

            if (account.Role == null)
            {
                return BadRequest("Rollen 'Employee' existerar inte i databasen");
            }

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAllAccounts), new { id = account.Id }, account);
        }

        // Hämta alla konton (Endast Admin) Hämtar UserName och RoleName endast
        [HttpGet]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<IEnumerable<object>>> GetAllAccounts()
        {
            var accounts = await _context.Accounts
                .Include(a => a.Role)
                .Select(a => new
                {
                    a.UserName,
                    Role = a.Role.RoleName
                })
                .ToListAsync();

            return Ok(accounts);
        }

        // Admin: Uppdatera Konton
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] UpdateAccountRequest request)
        {
            var existingAccount = await _context.Accounts.FindAsync(id);
            if (existingAccount == null)
            {
                return NotFound("Kontot hittades inte.");
            }

            // Uppdatera lösenord OM det skickas med
            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                existingAccount.Password =  BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }

            // Uppdatera användarnamn OM det skickas med
            if (!string.IsNullOrWhiteSpace(request.NewUserName) && existingAccount.UserName != request.NewUserName)
            {
                if (await _context.Accounts.AnyAsync(a => a.UserName == request.NewUserName))
                {
                    return BadRequest("Användarnamnet är redan taget.");
                }

                existingAccount.UserName = request.NewUserName;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Admin: Ta bort ett konto
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
