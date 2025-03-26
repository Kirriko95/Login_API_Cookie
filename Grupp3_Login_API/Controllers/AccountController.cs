using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Grupp3_Login_API.Models;
using Grupp3_Login_API.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using BCrypt.Net;
using System.Reflection.Metadata.Ecma335;

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
        public async Task<ActionResult<Account>> Register(CreateAccountDto createAccountDto)
        {
            var newAccount = new Account
            {
                UserName = createAccountDto.UserName,
                Password = BCrypt.Net.BCrypt.HashPassword(createAccountDto.Password),
                RoleId = 3
            };

            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kontot skapades framgångsrikt" });
            
        }

        // Admin kan skapa ett Employee-konto (Får automatiskt rollen "Employee")
        [HttpPost("create-employee")]
        [Authorize(Roles = "Admin")] // Endast admin kan skapa employee-konton
        public async Task<ActionResult<Account>> CreateEmployee(CreateAccountDto createAccountDto)
        {
            var newAccount = new Account
            {
                UserName = createAccountDto.UserName,
                Password = BCrypt.Net.BCrypt.HashPassword(createAccountDto.Password),
                RoleId = 2
            };

            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kontot skapades framgångsrikt." });
        }


        // Hämta alla konton (Endast Admin) Hämtar UserName och RoleName endast
        [HttpGet]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult> GetAllAccounts()
        {
            var allAccounts = await _context.Accounts
                .Where(a => a.Role.RoleName != "Admin")
                .Select(a => new
                {
                    a.Id,
                    a.UserName,
                    Role = a.Role.RoleName
                })
                .ToListAsync();
            
            return Ok(allAccounts);
        }
        // ✅ Hämta konto baserat på id
        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccountById(int id)
        {
            var account = await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (account == null)
            {
                return NotFound(); // Om kontot inte finns
            }

            var accountView = new AccountView
            {
                Id = account.Id,
                UserName = account.UserName,
                Role = account.Role?.RoleName
            };

            return Ok(accountView);
        }
        // Admin: Uppdatera Konton
        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task <IActionResult> UpdateAccount(int id, UpdateAccountDto updateAccountDto)
        {
            var account = await _context.Accounts.FindAsync(id);

            if (account == null)
            {
                return NotFound();
            }

            account.UserName = updateAccountDto.UserName;
            account.Password = BCrypt.Net.BCrypt.HashPassword(updateAccountDto.Password);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Kontot uppdaterades framgångsriklt." });
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
