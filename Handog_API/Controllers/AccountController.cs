using Handog_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Handog_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/account/signup
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] Account account)
        {
            // Check if email already exists
            var existing = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email == account.Email);

            if (existing != null)
            {
                return BadRequest(new { message = "Email already registered." });
            }

            // Default values
            account.AccountStatus = "Active";
            account.AbsenceCount = 0;

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Signup successful." });
        }

        // POST: api/account/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email == request.Email && a.AccPassword == request.Password);

            if (account == null)
            {
                return Unauthorized(new { message = "Invalid credentials." });
            }

            if (account.AccountStatus != "Active")
            {
                return Unauthorized(new { message = "Inactive account." });
            }

            return Ok(new 
            { Message = "Login successful.",
                AccountNum = account.AccountNum,
                AccRole = account.AccRole,
                Firstname = account.Firstname,
                Lastname = account.Lastname
            });
        }
    }

    // DTO for login
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
