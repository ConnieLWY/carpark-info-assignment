using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using HandshakesByDC_BEAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HandshakesByDC_BEAssignment.Controllers.v2
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration config, AppDbContext context, ILogger<AuthController> logger)
        {
            _config = config;
            _context = context;
            _logger = logger;
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest("Username and password are required.");
                }

                // Find user and verify password
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null)
                {
                    return Unauthorized("Invalid username or password");
                }

                // Verify password hash
                var hashedPassword = HashPassword(request.Password);
                if (user.PasswordHash != hashedPassword)
                {
                    return Unauthorized("Invalid username or password");
                }

                // Generate JWT token
                var token = GenerateJwtToken(user);

                var response = new LoginResponse
                {
                    Token = token,
                    Username = user.Username
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during login: {ex.Message}");
                return StatusCode(500, "Internal server error during login");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Email, user.Email),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3), // Token expires in 3 hours
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Optional: Endpoint to validate if token is still valid
        [Authorize]
        [HttpGet("validate-token")]
        public IActionResult ValidateToken()
        {
            try
            {
                var currentUser = HttpContext.User;

                // Extract claims using the same claim types used when creating the token
                var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = currentUser.FindFirst(ClaimTypes.Name)?.Value;
                var email = currentUser.FindFirst(ClaimTypes.Email)?.Value;

                return Ok(new
                {
                    userId = userId,
                    username = username,
                    email = email
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}